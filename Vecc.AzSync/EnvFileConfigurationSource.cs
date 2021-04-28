using Microsoft.Extensions.Configuration;

namespace Vecc.AzSync
{
    public class EnvFileConfigurationSource : IConfigurationSource
    {
        private readonly string _file;

        public EnvFileConfigurationSource() : this("env")
        {
        }

        public EnvFileConfigurationSource(string file)
        {
            this._file = file;
        }

        public IConfigurationProvider Build(IConfigurationBuilder builder)
        {
            var fileProvider = builder.GetFileProvider();
            return new EnvFileConfigurationProvider(this._file, fileProvider);
        }
    }
}
