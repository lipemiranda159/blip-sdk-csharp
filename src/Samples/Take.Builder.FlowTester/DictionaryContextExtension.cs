using Lime.Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Take.Blip.Client.Extensions.Context;

namespace Take.Builder.FlowTester
{
    internal class DictionaryContextExtension :  IContextExtension
    {
        private Dictionary<string, Document> valuesDictionary;

        public DictionaryContextExtension(Dictionary<string, Document> valuesDictionary)
        {
            this.valuesDictionary = valuesDictionary;
        }

        public Task DeleteGlobalVariableAsync(string variableName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task DeleteVariableAsync(Identity identity, string variableName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<DocumentCollection> GetIdentitiesAsync(int skip = 0, int take = 100, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        public async Task<T> GetVariableAsync<T>(Identity identity, string variableName, CancellationToken cancellationToken) where T : Document
        {
            Document result;
            valuesDictionary.TryGetValue(variableName, out result);
            return (T)result;
        }

        public async Task<DocumentCollection> GetVariablesAsync(Identity identity, int skip = 0, int take = 100, CancellationToken cancellationToken = default)
        {
            throw new Exception();
            
        }

        public async Task SetGlobalVariableAsync<T>(string variableName, T document, CancellationToken cancellationToken, TimeSpan expiration = default) where T : Document
        {
            valuesDictionary.Add(variableName, document);
        }

        public async Task SetVariableAsync<T>(Identity identity, string variableName, T document, CancellationToken cancellationToken, TimeSpan expiration = default) where T : Document
        {
            valuesDictionary.Add(variableName, document);
        }
    }
}