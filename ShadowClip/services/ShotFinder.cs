using System.Collections.Generic;
using System.Threading.Tasks;

namespace ShadowClip.services
{
    public interface IShotFinder
    {
        Task<IEnumerable<double>> GetShotTimes(string videoFilePath);
    }

    public class ShotFinder : IShotFinder
    {
        //This class gets created by the DI container.  Uncomment the contructor if you need to pull in a dependency via DI.
        /* 
        private readonly ISettings _settings;
        public ShotFinder(ISettings settings)
        {
            _settings = settings;
        }
        */

        public async Task<IEnumerable<double>> GetShotTimes(string videoFilePath)
        {
            await Task.Delay(3000); //Lots of number crunching in the back ground

            return new[] {15.0, 30, 45}; //Shots found at 15, 30 ,45
        }
    }
}