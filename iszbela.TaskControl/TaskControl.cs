using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace iszbela.TaskControl
{

    /// <summary>
    /// It handles the chained control flow of synchronous/asynchronous tasks.
    /// </summary>
    public sealed class TaskControl : IDisposable
    {
        /// <summary>
        /// For regulating parallel processes.
        /// </summary>
        private object _lock = new object();

        /// <summary>
        /// A list containing parallel tasks.
        /// </summary>
        public List<List<Task>> tasks = new List<List<Task>>();

        private bool _disposed = false;
        private CancellationTokenSource _disposeCancellationTokenSource = new CancellationTokenSource();


        /// <summary>
        /// Adding synchronous/asynchronous tasks to the list.
        /// </summary>
        private void Add(Action action, int type, CancellationToken token)
        {
            //Regulating the addition of multiple tasks, where one executes while the others wait their turn.
            lock (_lock)
            {
                if (!_disposed)
                {
                    CancellationTokenSource cancellation = CancellationTokenSource.CreateLinkedTokenSource(_disposeCancellationTokenSource.Token, token);

                    //elsõ inditás esetén
                    if (tasks.Count == 0)
                    {
                        List<Task> groupf = new List<Task>();  //The first is the sync group. 
                        groupf.Add(Task.Run(() => { }, cancellation.Token));
                        tasks.Add(groupf);
                    }

                    List<Task> group = tasks.Last(); //The last group.

                    if (type == 0) //sync
                    {
                        List<Task> newgroup = new List<Task>();
                        newgroup.Add(Task.WhenAll(group).ContinueWith((x) =>
                        {
                            lock (_lock)
                            {
                                if (!_disposed)
                                {
                                    group.Clear();
                                    tasks.Remove(group);
                                    group = null;
                                }
                            }
                            action();
                        }, cancellation.Token));
                        tasks.Add(newgroup);
                    }
                    else // async
                    {
                        if (group.Count > 1) // Async, the current group.
                        {
                            group.Add(
                                Task.WhenAll(group[0]).ContinueWith((x) =>  //When the first async task starts, the others start as well.
                                {
                                    action();
                                }, cancellation.Token));
                        }
                        else
                        { //Sync, the current group.
                          // I create a new async group and add it to the list.
                          // In this group, the first task will delete the previous sync references, while the others will not.
                            List<Task> newgroup = new List<Task>();

                            newgroup.Add(new Task(() => { }, cancellation.Token)); //Because two tasks indicate they are async.
                            newgroup.Add(
                                Task.WhenAll(group).ContinueWith((x) =>
                                {
                                    lock (_lock)
                                    {
                                        if (!_disposed)
                                        {
                                            group.Clear();
                                            tasks.Remove(group);
                                            group = null;
                                        }
                                    }
                                    newgroup[0].Start();
                                    action();
                                }, cancellation.Token));
                            tasks.Add(newgroup);
                        }
                    }
                }
            }
        }

        /// <summary>
        ///Adding a synchronous task to the task list.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="token"></param>
        public void AddSync(Action action, CancellationToken token)
        {
            Add(action, 0, token);
        }


        /// <summary>
        /// Adding an asynchronous task to the task list.
        /// </summary>
        /// <param name="action"></param>
        /// <param name="token"></param>
        public void AddAsync(Action action, CancellationToken token)
        {
            Add(action, 1, token);
        }


        public void Dispose()
        {
            if (!_disposed)
            {
                lock (_lock)
                {
                    _disposed = true;
                    _disposeCancellationTokenSource.Cancel();
                    foreach (var task in tasks)
                    {
                        task.Clear();
                    }
                    tasks.Clear();
                    tasks = null;
                    _disposeCancellationTokenSource = null;
                }
                _lock = null;
            }
            GC.SuppressFinalize(this);
        }

        ~TaskControl()
        {
            Dispose();
        }

    }

}