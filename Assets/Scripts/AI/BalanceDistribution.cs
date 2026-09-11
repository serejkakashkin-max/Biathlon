using System;
using System.Collections.Generic;

namespace Biathlon.AI
{
    public sealed class BalanceDistribution
    {
        public int Count { get; } public double Mean { get; } public double Median { get; }
        public double StandardDeviation { get; } public double P05 { get; } public double P95 { get; }
        public double Minimum { get; } public double Maximum { get; }
        public BalanceDistribution(IEnumerable<double> source)
        {
            if(source==null)throw new ArgumentNullException();var a=new List<double>(source);if(a.Count==0)throw new ArgumentException("Nonempty sample required.");
            double mean=0,m2=0;int n=0;
            foreach(double x in a){if(double.IsNaN(x)||double.IsInfinity(x))throw new ArgumentException("Finite sample required.");n++;double d=x-mean;mean+=d/n;m2+=d*(x-mean);}
            a.Sort();Count=n;Mean=mean;StandardDeviation=Math.Sqrt(m2/n);Minimum=a[0];Maximum=a[n-1];Median=Quantile(a,.5);P05=Quantile(a,.05);P95=Quantile(a,.95);
        }
        private static double Quantile(List<double> a,double p){double index=(a.Count-1)*p;int lo=(int)index;return a[lo]+(a[Math.Min(lo+1,a.Count-1)]-a[lo])*(index-lo);}
        public static double? Correlation(IReadOnlyList<double> x,IReadOnlyList<double> y)
        {
            if(x==null||y==null||x.Count!=y.Count||x.Count<2)throw new ArgumentException("Matched samples required.");
            double mx=0,my=0,xx=0,yy=0,xy=0;
            for(int i=0;i<x.Count;i++){if(double.IsNaN(x[i])||double.IsInfinity(x[i])||double.IsNaN(y[i])||double.IsInfinity(y[i]))throw new ArgumentException("Finite samples required.");double dx=x[i]-mx,dy=y[i]-my;mx+=dx/(i+1);my+=dy/(i+1);xx+=dx*(x[i]-mx);yy+=dy*(y[i]-my);xy+=dx*(y[i]-my);}
            return xx==0||yy==0?(double?)null:xy/Math.Sqrt(xx*yy);
        }
        public static double PairedBenefit(double baselineSeconds,double changedSeconds,double delta=1)
        {if(double.IsNaN(baselineSeconds)||double.IsInfinity(baselineSeconds)||baselineSeconds<=0||double.IsNaN(changedSeconds)||double.IsInfinity(changedSeconds)||changedSeconds<=0||double.IsNaN(delta)||double.IsInfinity(delta)||delta<=0)throw new ArgumentException("Positive finite matched observations required.");return (baselineSeconds-changedSeconds)/delta;}
    }
}
