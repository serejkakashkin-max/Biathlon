# TASK-071B4C — Swept junction guard occupancy

Extend junction queries to explicit forward swept distance and whole-body half extent. This
must detect crossings when both endpoints are outside, wrap closed seams and preserve whole
circuits whose endpoints coincide. Guard bounds are conservatively inclusive. Negative travel,
foreign points and open-loop overrun fail. Branch changes must be split by callers; these
queries neither grant passage nor reserve junctions. Test tunnelling, body clearance and GC.
