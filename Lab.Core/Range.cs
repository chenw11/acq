using System;
using System.Collections.Generic;
using System.Configuration;

namespace Lab
{
    public class Range<T> where T:IComparable<T>
    {
        protected const string errorOrdering = "Invalid range: the minimum must be less than the maximum!";

        protected T pMin;
        protected T pMax;

        protected void AssertValidAndSet(T attemptedMin, T attemptedMax)
        {
            if (attemptedMin.CompareTo(attemptedMax) < 0)
            {
                pMin = attemptedMin;
                pMax = attemptedMax;
            }
            else
                throw new ArgumentException(errorOrdering);
        }

        public virtual bool Allows(T value) 
        { 
            return (pMin.CompareTo(value) <= 0) && (value.CompareTo(pMax) <= 0);
        }

        public virtual T Maximum { get { return pMax; } set { AssertValidAndSet(pMin, value); } }
        public virtual T Minimum { get { return pMin; } set { AssertValidAndSet(value, pMax); } }

        public Range(T min, T max)
        {
            AssertValidAndSet(min, max);
        }
        public override string ToString()
        {
            return String.Format("[{0}, {1}]", Minimum.ToString(), Maximum.ToString());
        }

        public static Range<T> Parse(string s, Converter<string, T> parser)
        {
            int n = s.Length;
            if ((s[0] != '[') || (s[n - 1] != ']'))
                throw new ArgumentException("Invalid range.");
            string[] a = s.Split(new char[] { ',', ' ', '[', ']' }, StringSplitOptions.RemoveEmptyEntries);
            if (a.Length != 2)
                throw new ArgumentException("Range must have 2 components.");
            T[] b = Array.ConvertAll(a, parser);
            return new Range<T>(b[0], b[1]);
        }

        public static Range<T> ComputeRange(IEnumerable<T> dataSet)
        {
            List<T> list = new List<T>(dataSet);
            list.Sort();
            return new Range<T>(list[0], list[list.Count - 1]);
        }
    }
}