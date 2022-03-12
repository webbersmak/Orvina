// See https://aka.ms/new-console-template for more information


while (true)
{
    var running = true;
    var hitCount = 0;
    var firstHitCount = -1;
    using (var orveng = new Orvina.Engine.SearchEngine())
    {
        orveng.OnFileFound += (s, e) =>
        {
            Console.WriteLine($"[{++hitCount}]{s}");
        };

        orveng.OnSearchComplete += () =>
        {

            if (firstHitCount == -1)
            {
                firstHitCount = hitCount;
            }
            else if (hitCount != firstHitCount)
            {
                //big oopsie
                System.Diagnostics.Debugger.Break();
            }


            running = false;
            Console.WriteLine("finished");
        };

        orveng.Start(@"C:\users", true, "orvina", includeHidden: false, ".cs");

        while (running)
        {
            System.Threading.Thread.Sleep(100);
        }
    }

    if (Console.KeyAvailable)
        break;
}
