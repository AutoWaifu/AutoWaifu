using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WaifuLog.LogFormatters
{
    public class WarningLogFormatter
    {
        public virtual string Format(string warning)
        {
            return warning;
        }
    }
}
