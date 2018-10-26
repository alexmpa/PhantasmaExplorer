﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Phantasma.Cryptography;
using Phantasma.Explorer.Infrastructure.Interfaces;
using Phantasma.Explorer.Utils;
using Phantasma.Explorer.ViewModels;

namespace Phantasma.Explorer.Controllers
{
    public class HomeController
    {
        public IRepository Repository { get; set; }

        public HomeController(IRepository repo)
        {
            Repository = repo;
        }

        public HomeViewModel GetLastestInfo()
        {
            var blocks = new List<BlockViewModel>();
            var txs = new List<TransactionViewModel>();
            foreach (var block in Repository.GetBlocks())
            {
                blocks.Add(BlockViewModel.FromBlock(Repository, block));
            }

            var chart = new Dictionary<string, uint>();

            foreach (var transaction in Repository.GetTransactions())
            {
                var tempBlock = Repository.GetBlock(transaction);
                txs.Add(TransactionViewModel.FromTransaction(Repository, BlockViewModel.FromBlock(Repository, tempBlock), transaction));
            }

            // tx history chart calculation
            var repTxs = Repository.GetTransactions(null, 1000);
            foreach (var transaction in repTxs)
            {
                var tempBlock = Repository.GetBlock(transaction);

                DateTime chartTime = tempBlock.Timestamp;
                var chartKey = $"{chartTime.Day}/{chartTime.Month}";

                if (chart.ContainsKey(chartKey))
                {
                    chart[chartKey] += 200;
                }
                else
                {
                    chart[chartKey] = 1;
                }
            }

            int totalChains = Repository.GetAllChains().Count; //todo repo
            uint height = Repository.GetChainByName("main").BlockHeight; //todo repo
            int totalTransactions = 0; //todo

            var vm = new HomeViewModel
            {
                Blocks = blocks.OrderByDescending(b => b.Timestamp).ToList(),
                Transactions = txs,
                Chart = chart,
                TotalTransactions = totalTransactions,
                TotalChains = totalChains,
                BlockHeight = height,
            };
            return vm;
        }

        public List<CoinRateViewModel> GetRateInfo()
        {
            var symbols = new[] { "USD", "BTC", "ETH", "NEO" };

            var tasks = symbols.Select(symbol => CoinUtils.GetCoinInfoAsync(CoinUtils.SoulId, symbol));
            var rates = Task.WhenAll(tasks).GetAwaiter().GetResult();

            var coins = new List<CoinRateViewModel>();
            for (int i = 0; i < rates.Length; i++)
            {
                var coin = new CoinRateViewModel
                {
                    Symbol = symbols[i],
                    Rate = rates[i]["quotes"][symbols[i]].GetDecimal("price"),
                    ChangePercentage = rates[i]["quotes"][symbols[i]].GetDecimal("percent_change_24h")
                };
                coins.Add(coin);
            }

            return coins;
        }

        public string SearchCommand(string input)
        {
            try
            {
                if (Address.IsValidAddress(input)) //maybe is address
                {
                    return $"address/{input}";
                }

                var token = Repository.GetToken(input.ToUpperInvariant());
                if (token != null)// token
                {
                    return $"token/{token.Symbol}";
                }

                var chain = Repository.GetChainByName(input) ?? Repository.GetChain(input);
                if (chain != null)
                {
                    return $"chain/{chain.Address.Text}";
                }

                var hash = Hash.Parse(input);
                if (hash != null)
                {
                    var tx = Repository.GetTransaction(hash.ToString());
                    if (tx != null)
                    {
                        return $"tx/{tx.Hash}";
                    }

                    var block = Repository.GetBlock(hash.ToString());
                    if (block != null)
                    {
                        return $"block/{block.Hash}";
                    }
                }

                return null;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return "/home";
            }
        }
    }
}
