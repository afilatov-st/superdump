﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using SuperDump.Models;
using SuperDumpService.Helpers;
using SuperDumpService.Models;

namespace SuperDumpService.Services.Clustering {
	/// <summary>
	/// This class holds clusters of dumps that are similar to each other to some degree.
	/// This repository class is supposed to be the API to be accessed from within the rest of superdump.
	/// 
	/// Peridically re-run cluster generation.
	/// </summary>
	public class DumpClusterRepository {
		private readonly DumpRepository dumpRepository;
		private readonly RelationshipRepository relationshipRepository;

		public DumpClusterHeap DumpClusterHeap { get; private set; }

		public DumpClusterRepository(DumpRepository dumpRepository, RelationshipRepository relationshipRepository) {
			this.dumpRepository = dumpRepository;
			this.relationshipRepository = relationshipRepository;
			this.DumpClusterHeap = new DumpClusterHeap(Enumerable.Empty<DumpCluster>()); // initialize empty to avoid NPE
		}

		public void StartHangfireJob() {
			RecurringJob.AddOrUpdate(() => UpdateClusterHeapSync(), Cron.Minutely);
		}

		[Queue("updateclusterheap")]
		public void UpdateClusterHeapSync() {
			AsyncHelper.RunSync(UpdateClusterHeap);
		}

		/// <summary>
		/// Calculate new cluster heap and replace in repository when finished
		/// </summary>
		/// <returns></returns>
		private async Task UpdateClusterHeap() {
			var calc = new DumpClusterCalculator();
			var newClusterHeap = await calc.CalculateClusters(dumpRepository.GetAll(), relationshipRepository);

			// replace
			this.DumpClusterHeap = await newClusterHeap.ToClusterHeap(dumpRepository);
		}
	}
}
