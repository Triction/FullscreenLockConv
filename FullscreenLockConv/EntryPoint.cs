// Part of: https://stackoverflow.com/a/19326

using System;

namespace FullscreenLockConv
{
    public static class EntryPoint
    {
        [STAThread]
        public static void Main(string[] args)
        {
            var manager = new SingleInstanceManager();
            manager.Run(args);
        }
    }
}
