using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;

namespace ITGeoTagger
{
    public class IT_ThreadManager
    {
        public int PostThreadReleaseBusyMAX = 2;
        public List<Thread> PostThreads = new List<Thread>();
        public List<Thread> PreThreads = new List<Thread>();

        public Thread PreProcessThread;
        public Thread PostProcessThread;
        
        private  System.Windows.Forms.Timer thread_Checker;

        public int PostThreadReleaseBusyCount = 0;

        public IT_ThreadManager(ITGeotagger parent) {

            //start the thread timer with initial delay of 10 seconds

            this.thread_Checker = new System.Windows.Forms.Timer();
            this.thread_Checker.Tick += new EventHandler(Thread_Checker_Tick);
            this.thread_Checker.Interval = 10000;
            thread_Checker.Start();
        }
        private void Thread_Checker_Tick(object sender, EventArgs e)
        {
            thread_Checker.Enabled = false;
            thread_Checker.Interval = 1000;
            if (PreProcessThread == null)
            {
                PreProcessThread = new Thread(DefaultFunction);
            }
            if (PostProcessThread == null)
            {
                PostProcessThread = new Thread(DefaultFunction);
            }

            //check threads
            if (PreThreads.Count > 0) // priotise the prprocessing threads
            {

                if ((!PreProcessThread.IsAlive) && (!PostProcessThread.IsAlive))
                { //check if thread is live or we need a flag here
                    PreProcessThread = PreThreads[0];
                    GC.Collect(); //garbage collection
                    PreProcessThread.Start(); //start new background thread
                    PreThreads.RemoveAt(0);
                }
            }
            else if (PostThreads.Count > 0)
            {

                if ((!PreProcessThread.IsAlive) && (!PostProcessThread.IsAlive) && (this.PostThreadReleaseBusyCount < this.PostThreadReleaseBusyMAX)) //check if thread is alive
                {
                    PostProcessThread = PostThreads[0]; //set thread to new thread
                    GC.Collect(); //garbage collection
                    PostProcessThread.Start(); //start new background thread
                    PostThreads.RemoveAt(0);
                }

            }

            thread_Checker.Enabled = true;
            thread_Checker.Start();
        }

        public void DefaultFunction() { }
    }
}
