using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    [DataContract]
    public sealed class PhysicalConnection
    {
        private PhysicalConnection() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "junction_id", IsRequired = true)] public string JunctionId { get; private set; }
        [DataMember(Name = "from_port_id", IsRequired = true)] public string FromPortId { get; private set; }
        [DataMember(Name = "to_port_id", IsRequired = true)] public string ToPortId { get; private set; }
        [DataMember(Name = "kind", IsRequired = true)] public string Kind { get; private set; }
        internal void Validate()
        {
            TrackValidation.Id(Id); TrackValidation.Id(JunctionId);
            TrackValidation.Id(FromPortId); TrackValidation.Id(ToPortId);
            if (Kind != "coincident_seam" || FromPortId == ToPortId)
                throw new ArgumentException("Only explicit coincident seams between distinct ports are supported; finite connectors need their own physical path.");
        }
    }

    /// <summary>Authored directed connectivity, separate from shared conflict-region membership.</summary>
    [DataContract]
    public sealed class PhysicalConnectionDefinition
    {
        private PhysicalConnectionDefinition() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "junction_definition_id", IsRequired = true)] public string JunctionDefinitionId { get; private set; }
        [DataMember(Name = "connections", IsRequired = true)] private PhysicalConnection[] Connections { get; set; }
        public string JunctionSnapshotJson { get; private set; }
        public string TrackSnapshotJson { get; private set; }
        public int Count => Connections.Length;
        public PhysicalConnection Get(int index) => Connections[index];

        public static PhysicalConnectionDefinition FromJson(string json, PhysicalJunctionDefinition junctions)
        {
            if (junctions == null) throw new ArgumentNullException(nameof(junctions));
            var value = RouteJson.Read<PhysicalConnectionDefinition>(json);
            if (value == null || value.SchemaVersion != 1 || value.Connections == null)
                throw new SerializationException("Invalid physical connection schema.");
            TrackValidation.Id(value.Id);
            if (value.JunctionDefinitionId != junctions.Id) throw new ArgumentException("Wrong junction definition.");
            var ids = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < value.Count; i++)
            {
                var connection = value.Get(i);
                if (connection == null) throw new ArgumentException("Null connection.");
                connection.Validate();
                if (!ids.Add(connection.Id)) throw new ArgumentException("Duplicate connection ID.");
                PhysicalJunction junction = null;
                for (int j = 0; j < junctions.Count; j++)
                    if (junctions.Get(j).Id == connection.JunctionId) junction = junctions.Get(j);
                if (junction == null) throw new ArgumentException("Unknown connection junction.");
                PhysicalJunctionPort from = null, to = null;
                for (int p = 0; p < junction.PortCount; p++)
                {
                    var port = junction.GetPort(p);
                    if (port.Id == connection.FromPortId) from = port;
                    if (port.Id == connection.ToPortId) to = port;
                }
                if (from == null || to == null || from.Anchor.LoopId == to.Anchor.LoopId)
                    throw new ArgumentException("Seam must join explicit ports on different loops in the same junction.");
                for (int j = 0; j < i; j++)
                    if (value.Get(j).FromPortId == connection.FromPortId && value.Get(j).ToPortId == connection.ToPortId)
                        throw new ArgumentException("Duplicate directed connection.");
            }
            value.JunctionSnapshotJson = junctions.ToJson(); value.TrackSnapshotJson = junctions.TrackSnapshotJson;
            return value;
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
