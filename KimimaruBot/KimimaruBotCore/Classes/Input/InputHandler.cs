﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace KimimaruBot
{
    /// <summary>
    /// Handles carrying out input sequences.
    /// </summary>
    public static class InputHandler
    {
        public static void CarryOutInput(List<List<Parser.Input>> inputList)
        {
            /*Kimimaru: We're using a thread pool for efficiency
             * Though very unlikely, there's a chance the input won't execute right away if it has to wait for a thread to be available
             * However, there are often plenty of available threads, so this shouldn't be an issue since we use only one thread per input string
             * Uncomment the following lines to see how many threads are supported in the pool on your machine */
            //ThreadPool.GetMinThreads(out int workermin, out int completionmin);
            //ThreadPool.GetMaxThreads(out int workerthreads, out int completionPortThreads);
            //Console.WriteLine($"Min workers: {workermin} Max workers: {workerthreads} Min async IO threads: {completionmin} Max async IO threads: {completionPortThreads}");

            ThreadPool.QueueUserWorkItem(new WaitCallback(InputThread), inputList);

            //Next step; set a bool that's checked in the input threads to stop all ongoing inputs immediately
        }

        private static void InputThread(object obj)
        {
            List<List<Parser.Input>> inputList = (List<List<Parser.Input>>)obj;

            Stopwatch sw = new Stopwatch();

            List<int> indices = new List<int>(16);

            for (int i = 0; i < inputList.Count; i++)
            {
                List<Parser.Input> inputs = inputList[i];

                indices.Clear();

                //Press all buttons unless it's a release input
                for (int j = 0; j < inputs.Count; j++)
                {
                    indices.Add(j);
                    Parser.Input input = inputs[j];

                    if (input.release == true)
                    {
                        VJoyController.Joystick.ReleaseInput(input);
                    }
                    else
                    {
                        VJoyController.Joystick.PressInput(input);
                    }
                }

                sw.Start();

                while (indices.Count > 0)
                {
                    //Release buttons when we should
                    for (int j = indices.Count - 1; j >= 0; j--)
                    {
                        Parser.Input input = inputs[indices[j]];

                        if (sw.ElapsedMilliseconds < input.duration) continue;

                        if (input.hold == false)
                        {
                            VJoyController.Joystick.ReleaseInput(input);
                        }

                        indices.RemoveAt(j);
                    }
                }

                sw.Reset();
            }

            //At the end of it all, release every input
            for (int i = 0; i < inputList.Count; i++)
            {
                List<Parser.Input> inputs = inputList[i];
                for (int j = 0; j < inputs.Count; j++)
                {
                    VJoyController.Joystick.ReleaseInput(inputs[j]);
                }
            }
        }
    }
}
