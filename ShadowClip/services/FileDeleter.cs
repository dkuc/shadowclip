using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IFileDeleter
    {
        void OnDelete(Func<FileInfo, Task> preDeleteTask);
        Task Delete(FileInfo file);
    }

    public class FileDeleter : IFileDeleter
    {
        private readonly List<Func<FileInfo, Task>> _preDeleteTasks = new List<Func<FileInfo, Task>>();

        public void OnDelete(Func<FileInfo, Task> preDeleteTask)
        {
            _preDeleteTasks.Add(preDeleteTask);
        }

        public async Task Delete(FileInfo file)
        {
            foreach (var preDeleteTask in _preDeleteTasks)
            {
                await preDeleteTask(file);
            }
            file.Delete();
        }
    }
}