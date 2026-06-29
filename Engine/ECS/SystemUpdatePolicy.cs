using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Core.ECS
{
    public enum SystemUpdatePolicy
    {
        FrameUpdate, // Variable time: runs once per render frame
        TickUpdate, // Fixed time: runs on a locked simulation step
        FixedUpdate, // Custom intervals: runs every X seconds
        EntityUpdate, // Reactive time: runs only when components/entities mutate
        Manual // Driven time: only runs when explicitly invoked(though GameEvents do exist so that might be a better option)


    }
}
