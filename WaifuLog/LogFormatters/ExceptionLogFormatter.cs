using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace WaifuLog.LogFormatters
{
    public class ExceptionLogFormatter
    {
        public virtual string Format(string message, Exception e)
        {
            string exceptionInfo = e.ToString();

            //  Remove personal file information from log details
            string result = $"{message} Details: {exceptionInfo}";

            var filePathRegex = new Regex(@"\w:\\[\w\s\\\.]+");

            foreach (Match m in filePathRegex.Matches(result))
            {
                string filePath = m.Value;
                string fileName = Path.GetFileName(filePath);
                result = result.Replace(filePath, fileName);
            }

            return result;
        }
    }
}
