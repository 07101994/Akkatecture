﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.Cluster.Sharding;
using Akka.Configuration;
using Akkatecture.Clustering.Configuration;
using Akkatecture.Clustering.Core;
using Akkatecture.Examples.UserAccount.Domain.UserAccountModel;

namespace Akkatecture.Examples.Worker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            //Get configuration file using Akkatecture's defaults as fallback
            var path = Environment.CurrentDirectory;
            var configPath = Path.Combine(path, "worker.conf");
            var baseConfig = ConfigurationFactory.ParseString(File.ReadAllText(configPath));
            
            //specified amount of workers running on their own thread
            var amountOfWorkers = 10;

            //Create several workers with each worker port will be 6001, 6002,...
            var actorSystems = new List<ActorSystem>();
            foreach (var worker in Enumerable.Range(1, amountOfWorkers+1))
            {
                //Create worker with port 600X
                var config = ConfigurationFactory.ParseString($"akka.remote.dot-netty.tcp.port = 600{worker}");
                config = config
                    .WithFallback(baseConfig)
                    .WithFallback(AkkatectureClusteringDefaultSettings.DefaultConfig());
                var clustername = config.GetString("akka.cluster.name");
                var actorSystem = ActorSystem.Create(clustername, config);
                actorSystems.Add(actorSystem);
                
                //Start the aggregate cluster, all requests being proxied to this cluster will be 
                //sent here to be processed
                StartUserAccountCluster(actorSystem);
            }

            Console.WriteLine("Akkatecture.Examples.Workers Running");

            var quit = false;
            
            while (!quit)
            {
                Console.Write("\rPress Q to Quit.         ");
                var key = Console.ReadLine();
                quit = key?.ToUpper() == "Q";
            }

            //Shut down all the local actor systems
            foreach (var actorsystem in actorSystems)
            {
                actorsystem.Terminate().Wait();
            }
            Console.WriteLine("Akkatecture.Examples.Workers Exiting.");
        }

        public static void StartUserAccountCluster(ActorSystem actorSystem)
        {
            var cluster = ClusterFactory<UserAccountAggregateManager, UserAccountAggregate, UserAccountId>
                .StartAggregateCluster(actorSystem);
        }
        
    }
}
