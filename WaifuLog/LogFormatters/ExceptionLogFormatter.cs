using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaifuLog.LogFormatters
{
    public class ExceptionLogFormatter
    {
        public virtual string Format(string message, Exception e)
        {
            string exceptionInfo = e.ToString();

            return $"{message} Details: {exceptionInfo}";
        }
    }
}
