//  Copyright (c) 2021 Demerzel Solutions Limited
//  This file is part of the Nethermind library.
// 
//  The Nethermind library is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  The Nethermind library is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//  GNU Lesser General Public License for more details.
// 
//  You should have received a copy of the GNU Lesser General Public License
//  along with the Nethermind. If not, see <http://www.gnu.org/licenses/>.
// 

using System;
using System.Linq;
using Nethermind.Blockchain.Processing;
using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Core.Extensions;
using Nethermind.Dsl.Pipeline.Data;
using Nethermind.Dsl.Pipeline.Sources;

namespace Nethermind.Dsl.Pipeline.Builders
{
    public class EventElementsBuilder
    {
        private readonly IBlockProcessor _blockProcessor;

        public EventElementsBuilder(IBlockProcessor blockProcessor)
        {
            _blockProcessor = blockProcessor ?? throw new ArgumentNullException(nameof(blockProcessor));
        }

        public EventsSource<EventData> GetSourceElement()
        {
            return new(_blockProcessor);
        }
        
        public PipelineElement<EventData, EventData> GetConditionElement(string key, string operation, string value)
        {

            if (key.Equals("EventSignature", StringComparison.InvariantCultureIgnoreCase) && operation.Equals("IS"))
            {
                return new PipelineElement<EventData, EventData>(
                    condition: t => CheckEventSignature(t, value), 
                    transformData: t => t);
            }

            return operation switch
            {
                "IS" => new PipelineElement<EventData, EventData>(
                    condition: (t => t.GetType().GetProperty(key)?.GetValue(t)?.ToString()?.ToLowerInvariant() == value.ToLowerInvariant()),
                    transformData: (t => t)),
                "==" => new PipelineElement<EventData, EventData>(
                    condition: (t => t.GetType().GetProperty(key)?.GetValue(t)?.ToString()?.ToLowerInvariant() == value.ToLowerInvariant()),
                    transformData: (t => t)),
                "NOT" => new PipelineElement<EventData, EventData>(
                    condition: (t => t.GetType().GetProperty(key)?.GetValue(t)?.ToString()?.ToLowerInvariant() != value.ToLowerInvariant()),
                    transformData: (t => t)),
                "!=" => new PipelineElement<EventData, EventData>(
                    condition: (t => t.GetType().GetProperty(key)?.GetValue(t)?.ToString()?.ToLowerInvariant() != value.ToLowerInvariant()),
                    transformData: (t => t)),
                "CONTAINS" => new PipelineElement<EventData, EventData>(
                    condition: (l => CheckIfContains(l, key, value)),
                    transformData: (l => l)),
                _ => null
            };
        }
        
        public static bool CheckEventSignature(LogEntry log, string signature)
        {
            signature = signature.Replace(" ", string.Empty); //Keccak will be wrong if we don't remove white space chars
            var signatureHash = Keccak.Compute(signature);

            if (log == null) return false;

            return log.Topics.First() == signatureHash;
        }

        private bool CheckIfContains(LogEntry logEntry,string key ,string value)
        {
            key = key.ToLowerInvariant();
            return key switch
            {
                "data" => logEntry.Data.ToHexString().Contains(value),
                "topics" => logEntry.Topics.Contains(new Keccak(value)),
                _ => false
            };
        }
    }
}