using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;
using System.Diagnostics;
using Grpc.Core;
using Grpc.Net.Client;
using GrpcGreeterClient;

// The port number must match the port of the gRPC server.
using var channel = GrpcChannel.ForAddress("https://localhost:7043");
var client = new Greeter.GreeterClient(channel);

// var reply = await client.SayHelloAsync(new HelloRequest { Name = "GreeterClient" });
// Console.WriteLine("Greeting: " + reply.Message);
//
// var responseTest = await client.CalculateSumAsync(new CalculateRequest { X= 12, Y= 24 });
// Console.WriteLine("result is: " + responseTest.Result);

var dies = 5;
var bumps_in_die = 694444;
var limit = 3000;

var call = client.GetBumps(new GetBumpsRequest { NumberOfDies= dies, BumpsInDie= bumps_in_die, LimitPerMessage = limit});

System.IO.DirectoryInfo di = new DirectoryInfo(@"D:\bumpsData\");
foreach (FileInfo file in di.GetFiles())
{
    file.Delete(); 
}
var current_die = 0;
var csv = new StringBuilder();
Stopwatch sw = new Stopwatch();

string filePath = @"D:\bumpsData\Die" + current_die + ".csv";
var firstLine = "bumpID,dieX,dieY,waferX,waferY,Type,Height,Cop,Die_ID,WaferID";
csv.AppendLine(firstLine);

await foreach (var response in call.ResponseStream.ReadAllAsync())
{
    sw.Start();
    if (current_die != response.DieId)
    {
        filePath = @"D:\bumpsData\Die" + current_die + ".csv";
        File.WriteAllText(filePath, csv.ToString());
        sw.Stop();
        Console.WriteLine($"***** Done writing die [{current_die}] ******");
        csv.Clear();
        current_die = response.DieId;
        csv.AppendLine(firstLine);
    }
    foreach (var bump in response.Bump)
    {
        var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8}, {9}", bump.Id, bump.DieXCord,
            bump.DieYCord, bump.WaferXCord, bump.WaferYCord, bump.Type, bump.Height, bump.Cop, bump.DieId,
            bump.WaferId);
        csv.AppendLine(newLine);
    }
    
}

filePath = @"D:\bumpsData\Die" + current_die + ".csv";
File.WriteAllText(filePath, csv.ToString());
sw.Stop();
Console.WriteLine($"***** Done writing die [{current_die}] ******");

// DONE !!!

//Console.WriteLine("Elapsed={0}",sw.Elapsed.TotalSeconds);
var speed = 54.145 * dies / sw.Elapsed.TotalSeconds;
Console.WriteLine($"***** Bumps Experiment - DIES: {dies}, Bumps in DIE: {bumps_in_die} ******");
Console.WriteLine("Total Size={0} GB",Math.Round(54.145 * dies / 1000, 2));
Console.WriteLine("SPEED with GRPC={0} MB/s", Math.Round(speed,2));
