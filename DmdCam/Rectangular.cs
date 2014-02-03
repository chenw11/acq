using Lab;
using System;

namespace eas_lab.acq.DmdCam
{
    public class Rectangular : NotifyPropertyChangedBase
    {
        public int Dim_X { get; private set; }
        public int Dim_Y { get; private set; }

        public Rectangular(int dim_x, int dim_y)
        {
            if ((dim_x < 16) || (dim_x > 4096))
                throw new ArgumentOutOfRangeException();
            if ((dim_y < 16) || (dim_y > 4096))
                throw new ArgumentOutOfRangeException();
            this.Dim_X = dim_x;
            this.Dim_Y = dim_y;
        }
    }
}
