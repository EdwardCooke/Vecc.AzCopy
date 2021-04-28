using System;

namespace Vecc.AzSync
{
    public class ProgressMonitor : IProgress<long>
    {
        private readonly FileMetaData _file;
        private decimal _lastReportedPercent = -1M;
        private bool _ended = false;
        private DateTime _timeStarted = DateTime.UtcNow;
        private long _lastReportedValue;

        public ProgressMonitor(FileMetaData file)
        {
            this._file = file;
        }

        public void Report(long value)
        {
            Console.CursorLeft = 0;
            //if we ever decide we need rate limiting we can do it here

            var completedPercent = Math.Floor(((decimal)value / this._file.Size) * 1000M) / 10M;
            if (completedPercent != this._lastReportedPercent)
            {
                this._lastReportedPercent = completedPercent;

                //extra spaces to clean up overruns
                //  100,000 KB/s vs 100 KB/s (100 KB/s if after 100,000 KB/s would look like 100 KB/sKB/s)
                Console.Write("  {0:000.0}% - {1} of {2} {3:###,###.##} KB/s       ", this._lastReportedPercent, value, this._file.Size, ((value - this._lastReportedValue) / (DateTime.UtcNow - this._timeStarted).TotalSeconds) / 1024);

                this._lastReportedValue = value;
                this._timeStarted = DateTime.UtcNow;
            }

            if (value == this._file.Size && !this._ended)
            {
                this._ended = true;
                Console.WriteLine();
            }
        }
    }
}
