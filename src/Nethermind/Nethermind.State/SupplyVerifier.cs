//  Copyright (c) 2018 Demerzel Solutions Limited
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

using Nethermind.Core;
using Nethermind.Core.Crypto;
using Nethermind.Dirichlet.Numerics;
using Nethermind.Logging;
using Nethermind.Serialization.Rlp;
using Nethermind.Trie;

namespace Nethermind.State
{
    public class SupplyVerifier : ITreeVisitor
    {
        private readonly ILogger _logger;
        private UInt256 _balance = UInt256.Zero;
        private int _accountsVisited;

        public SupplyVerifier(ILogger logger)
        {
            _logger = logger;
        }
            
        public bool ShouldVisit(Keccak nextNode) { return true; }

        public void VisitTree(Keccak rootHash, TrieVisitContext trieVisitContext) { }

        public void VisitMissingNode(Keccak nodeHash, TrieVisitContext trieVisitContext) { }

        public void VisitBranch(TrieNode node, TrieVisitContext trieVisitContext) { }

        public void VisitExtension(TrieNode node, TrieVisitContext trieVisitContext) { }

        public void VisitLeaf(TrieNode node, TrieVisitContext trieVisitContext, byte[] value = null)
        {
            if (trieVisitContext.IsStorage)
            {
                return;
            }
            
            AccountDecoder accountDecoder = new AccountDecoder();
            Account account = accountDecoder.Decode(node.Value.AsRlpStream());
            _balance += account.Balance;
            _accountsVisited++;
                
            _logger.Info($"Balance after visiting {_accountsVisited}: {_balance}");
        }

        public void VisitCode(Keccak codeHash, TrieVisitContext trieVisitContext) { }
    }
}