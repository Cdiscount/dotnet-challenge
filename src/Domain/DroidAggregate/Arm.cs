using System;
using Cds.DroidManagement.Domain.DroidAggregate.Abstractions;
using Cds.DroidManagement.Domain.DroidAggregate.ValueObjects;

namespace Cds.DroidManagement.Domain.DroidAggregate
{
    public class Arm
    {
        public ArmId SerialNumber { get; }

        private Arm(ArmId serialNumber)
        {
            SerialNumber = serialNumber;
        }

        public static Arm CreateNew()
        {
            return new Arm(Guid.NewGuid());
        }

        public static Arm FromDto(IArmDto droid)
        {
            if (droid == null)
            {
                return null;
            }

            return new Arm(droid.ArmId);
        }        
    }
}
