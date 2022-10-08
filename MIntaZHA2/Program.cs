using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MIntaZHA2
{
    class Program
    {
        public static class Util
        {
            public static Random rnd = new Random();
        }
        static void Main(string[] args)
        {
            List<Tagozat> tagozatok = Enumerable.Range(1, 5)
                .Select(t => new Tagozat()).ToList();
               
            var ts = tagozatok
                .Select(t => new Task(() => t.DoWork()
                , TaskCreationOptions.LongRunning)).ToList();

            List<Latogato> latogatok = Enumerable.Range(1, 30)
                .Select(l => new Latogato()).ToList();

            ts.AddRange(latogatok.Select(l => new Task(() =>
                {
                    l.Nezelod(tagozatok);
                }, TaskCreationOptions.LongRunning)).ToList());

            // kiiro todo
            ts.Add(new Task(() =>
            {
                while (tagozatok.Any(t => t.Status != Tagozat.Allapot.vege))
                {
                    Console.Clear();
                    latogatok.ForEach(
                        l => Console.WriteLine("ID: "+l.ID + " Státusz: " + l.Status + " erdeklődés: " + l.Erdeklodes
                        + " tagozatID: " + l.TagozatID));

                    tagozatok.ForEach(
                        t => Console.WriteLine("ID: "+t.ID + " hely: " + t.Hely + " Sztátusz: " + t.Status
                        +" Előadáscount:"+ t.EloadasCount)
                        );
                    Thread.Sleep(200);
                }
                
            },TaskCreationOptions.LongRunning));

           
            ts.ForEach(t => t.Start());


            Console.ReadKey();
        }

        public class Tagozat
        {           
            public enum Allapot
            {
                elindul, felkeszul, eload, diskural, vege
            }
            public int EloadasCount { get; set; }
            private static int nextId = 1;
            public int ID { get; set; }
            public int Hely { get; set; }
            public Allapot Status { get; set; }

            public Tagozat()
            {
                ID = nextId++;
                Hely = 10;
                Status = Allapot.elindul;
                EloadasCount = 1;
            }

            public void DoWork()
            {
                for (int i = 0; i < 8; i++)
                {
                    EloadasFolyamat();
                    EloadasCount++;
                }
                Status = Allapot.vege;
            }
            public void EloadasFolyamat()
            {
                Status = Allapot.felkeszul;
                Thread.Sleep(Util.rnd.Next(500, 1200));

                Status = Allapot.eload;
                Thread.Sleep(Util.rnd.Next(12000, 17000));

                Status = Allapot.diskural;
                Thread.Sleep(Util.rnd.Next(8000, 12000));
            }
        }

        public class Latogato
        {
            public static object lockObject = new object();
            public enum Allapot
            {
                foglalt, tavozna, szabad
            }

            private static int nextId = 1;
            public int ID { get; set; }
            public Allapot Status { get; set; }
            public int Erdeklodes { get; set; }
            public int TagozatID { get; set; }
            public Tagozat AktualisTagozat { get; set; }

            public Latogato()
            {
                ID = nextId++;
                Status = Allapot.szabad;
                Erdeklodes = 0;
            }

            public void Nezelod(List<Tagozat> tagozatok)
            {
                while (tagozatok.Any(t => t.Status != Tagozat.Allapot.vege))
                {
                    Tagozat t;
                    lock (lockObject)
                    {
                        t = tagozatok.Where(g =>
                        g.ID != this.TagozatID
                        && g.Hely > 0
                        && g.Status != Tagozat.Allapot.eload)
                            .FirstOrDefault();
                        if (t != null)
                        {
                            this.Atul(t);
                            t.Hely--;
                        }                      
                    }
                    Thread.Sleep(Util.rnd.Next(500, 1001));
                    if (this.Status == Allapot.foglalt)
                    {
                        ErdeklodesDekremental();
                    }                  
                    if (this.Status == Allapot.tavozna)
                    {
                        while (t.Status == Tagozat.Allapot.eload)
                        {
                            Thread.Sleep(1000);
                        }
                        lock (lockObject)
                        {
                            t.Hely++;                          
                        }
                        this.Status = Allapot.szabad;

                    }
                }
            }
            public void ErdeklodesDekremental()
            {
                while (Erdeklodes > 0)
                {
                    Thread.Sleep(1000);
                    int erd = Util.rnd.Next(1, 16);
                    if (Erdeklodes - erd < 0)
                    {
                        Erdeklodes = 0;
                    }
                    else
                    {
                        Erdeklodes -= erd;

                    }
                }
                this.Status = Allapot.tavozna;
                
            }

            public void Atul(Tagozat ujTagozat)
            {                              
                TagozatID = ujTagozat.ID;
                AktualisTagozat = ujTagozat;
                this.Status = Allapot.foglalt;
                this.Erdeklodes = 100;
            }

        }
    }
}
