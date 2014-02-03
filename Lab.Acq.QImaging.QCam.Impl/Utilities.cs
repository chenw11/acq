using QCamManagedDriver;
using System;

namespace Lab.Acq
{
    internal static class Utilities
    {
        /// <summary>
        /// Throws a semi-descriptive exception if the input is anything besides Success
        /// </summary>
        public static void Check(this QCamM_Err err)
        {
            if (err != QCamM_Err.qerrSuccess)
                throw new Exception("Qimaging camera error: " + err.ToString());
        }
    }
}
