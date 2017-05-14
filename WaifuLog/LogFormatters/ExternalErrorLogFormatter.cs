using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaifuLog.LogFormatters
{
    public class ExternalErrorLogFormatter
    {
        public virtual string Format(string externalError)
        {
            return externalError;
        }
    }
}
