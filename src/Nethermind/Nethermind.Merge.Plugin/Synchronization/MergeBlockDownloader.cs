﻿//  Copyright (c) 2021 Demerzel Solutions Limited
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
using Nethermind.Blockchain;
using Nethermind.Blockchain.Receipts;
using Nethermind.Consensus;
using Nethermind.Consensus.Validators;
using Nethermind.Core.Specs;
using Nethermind.Logging;
using Nethermind.Synchronization.Blocks;
using Nethermind.Synchronization.ParallelSync;
using Nethermind.Synchronization.Peers;
using Nethermind.Synchronization.Reporting;

namespace Nethermind.Merge.Plugin.Synchronization
{
    public class MergeBlockDownloader : BlockDownloader
    {
        private readonly IBeaconPivot _beaconPivot;
        private readonly IBlockTree _blockTree;
        private readonly ILogger _logger;

        public MergeBlockDownloader(
            IPoSSwitcher poSSwitcher,
            IBeaconPivot beaconPivot,
            ISyncFeed<BlocksRequest?>? feed, 
            ISyncPeerPool? syncPeerPool, 
            IBlockTree? blockTree,
            IBlockValidator? blockValidator, 
            ISealValidator? sealValidator, 
            ISyncReport? syncReport, 
            IReceiptStorage? receiptStorage,
            ISpecProvider? specProvider, 
            ILogManager logManager)
            : base(feed, syncPeerPool, blockTree, blockValidator, sealValidator, syncReport, receiptStorage, specProvider, new MergeBlocksSyncPeerAllocationStrategyFactory(poSSwitcher, logManager), logManager)
        {
            _blockTree = blockTree ?? throw new ArgumentNullException(nameof(blockTree));
            _beaconPivot = beaconPivot;
            _logger = logManager.GetClassLogger();
        }
        
        protected override long GetCurrentNumber(PeerInfo bestPeer)
        {
            long currentNumber = _beaconPivot.BeaconPivotExists()
                ? Math.Max(0, Math.Min(_blockTree.BestSuggestedBody.Number, bestPeer.HeadNumber - 1))
                : base.GetCurrentNumber(bestPeer);
            if (_logger.IsInfo) _logger.Info($"MergeBlockDownloader GetCurrentNumber: currentNumber {currentNumber}, beaconPivotExists: {_beaconPivot.BeaconPivotExists()}, BestSuggestedBody: {_blockTree.BestSuggestedBody.Number}");
            return currentNumber;
        }
        
        protected override long GetUpperDownloadBoundary(PeerInfo bestPeer, BlocksRequest blocksRequest)
        {
            long preMergeUpperDownloadBoundary = base.GetUpperDownloadBoundary(bestPeer, blocksRequest);
            long upperDownloadBoundary = _beaconPivot.BeaconPivotExists()
                ? Math.Min(preMergeUpperDownloadBoundary, _beaconPivot.PivotNumber)
                : preMergeUpperDownloadBoundary;
            if (_logger.IsInfo) _logger.Info($"MergeBlockDownloader GetUpperDownloadBoundary: {upperDownloadBoundary}, beaconPivotExists: {_beaconPivot.BeaconPivotExists()}, BestSuggestedBody: {_blockTree.BestSuggestedBody.Number}");
            return upperDownloadBoundary;
        }

        protected override bool ImprovementRequirementSatisfied(PeerInfo? bestPeer)
        {
            bool preMergeDifficultyRequirementSatisfied = base.ImprovementRequirementSatisfied(bestPeer);
            bool postMergeRequirementSatisfied = _beaconPivot.BeaconPivotExists() 
                                                 && Math.Min(bestPeer!.HeadNumber, _beaconPivot.PivotNumber) > (_blockTree.BestSuggestedBody?.Number ?? 0);
            bool improvementRequirementSatisfied = _beaconPivot.BeaconPivotExists()
                ? postMergeRequirementSatisfied
                : preMergeDifficultyRequirementSatisfied;
            
            if (_logger.IsInfo) _logger.Info($"MergeBlockDownloader GetUpperDownloadBoundary: {improvementRequirementSatisfied}, beaconPivotExists: {_beaconPivot.BeaconPivotExists()}, BestSuggestedBody: {_blockTree.BestSuggestedBody.Number}");
            return improvementRequirementSatisfied;
        }
    }
}