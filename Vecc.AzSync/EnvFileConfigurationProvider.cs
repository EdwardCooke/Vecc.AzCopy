using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;
using System.Collections.Generic;
using System.IO;

namespace Vecc.AzSync
{
    public class EnvFileConfigurationProvider : ConfigurationProvider
    {
        private readonly string _file;
        private readonly IFileProvider fileProvider;

        public EnvFileConfigurationProvider(string file, IFileProvider fileProvider)
        {
            this._file = file;
            this.fileProvider = fileProvider;
        }

        public override void Load()
        {
            string line;
            var fileContents = string.Empty;
            var variables = new Dictionary<string, string>();
            var file = this.fileProvider.GetFileInfo(this._file);

            if (file.Exists)
            {
                using var stream = file.CreateReadStream();
                using var streamReader = new StreamReader(stream);
                fileContents = streamReader.ReadToEnd();
            }

            using var stringReader = new StringReader(fileContents);

            while ((line = stringReader.ReadLine()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var parts = line.Split('=', 2);
                if (parts[0] == string.Empty)
                {
                    continue;
                }

                string value;
                if (parts.Length == 1)
                {
                    value = string.Empty;
                }
                else
                {
                    value = parts[1];
                }

                variables[parts[0]] = value;
            }

            base.Data = variables;
        }
    }
}
