using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cds.DroidManagement.Domain.DroidAggregate.Abstractions;
using Cds.DroidManagement.Domain.DroidAggregate.Commands;
using Cds.DroidManagement.Domain.DroidAggregate.Exceptions;
using Cds.DroidManagement.Domain.DroidAggregate.ValueObjects;

namespace Cds.DroidManagement.Domain.DroidAggregate
{
    public class Droid
    {
        public DroidId SerialNumber { get; } // WARNING: if Guid is not formated with date prefix, it will not be ordered in Db and auto-reindexing will cause perf troubles

        public DateTimeOffset CreatedOn { get; } // INFO: DateTimeOffset to include TimeZone

        public DroidName Name { get; private set; }

        public DroidNickname Nickname { get; private set; }

        public string Quote { get; private set; }

        public IReadOnlyCollection<Arm> Arms => _arms;

        private Droid(DroidId serialNumber, DateTimeOffset createdOn) // INFO: Private constructor prevent local identifier update (no private set)
        {
            SerialNumber = serialNumber;
            CreatedOn = createdOn;
        }

        public static async Task<Droid> CreateNewAsync(
            Func<DroidName, Task<bool>> nameAlreadyExists,
            Func<Task<string>> getRandomQuoteAsync,
            Func<string, string> encrypt,
            CreateDroid createDroid) // INFO: Usage of dependency injection
        {
            var droid = new Droid(Guid.NewGuid(), DateTimeOffset.Now) { _arms = new List<Arm>() };
            droid.SetNickname(createDroid.Nickname);
            await droid.SetQuoteAsync(getRandomQuoteAsync);
            droid.WithEncryptedQuote(encrypt, droid.Quote);
            return await droid.SetNameAsync(nameAlreadyExists, createDroid.Name);
        }

        public Task<Droid> UpdateAsync(Func<DroidName, Task<bool>> nameAlreadyExistsAsync, UpdateDroid updateDroid)
            => SetNickname(updateDroid.Nickname).SetNameAsync(nameAlreadyExistsAsync, updateDroid.Name);

        public Arm AddArm()
        {
            AssertLimitOfArmsNotReached(Arms.Count);

            var arm = Arm.CreateNew();
            _arms.Add(arm);
            return arm;
        }

        private async Task<Droid> SetNameAsync(Func<DroidName, Task<bool>> nameAlreadyExistsAsync, DroidName name)
        {
            var existingDroidWithThisName = await nameAlreadyExistsAsync(name);
            if (existingDroidWithThisName)
            {
                throw new DroidConflictNameException();
            }

            Name = name;
            return this;
        }

        private Droid SetNickname(DroidNickname nickname)
        {
            Nickname = nickname;
            return this;
        }

        private async Task<Droid> SetQuoteAsync(Func<Task<string>> getRandomQuoteAsync)
        {
            Quote = await getRandomQuoteAsync();
            return this;
        }

        private Droid WithEncryptedQuote(Func<string, string> encrypt, string quote)
        {
            Quote = encrypt(quote);
            return this;
        }

        // Reload domain from database
        public static Droid FromDto(IDroidDto droidDto)
        {
            if (droidDto == null)
            {
                throw new DroidNotFoundException();
            }

            return new Droid(droidDto.DroidId, droidDto.CreationDate)
            {
                Name = droidDto.Name,
                Nickname = droidDto.Nickname,
                Quote = droidDto.Quote
            };
        }

        public Droid WithArms(IEnumerable<IArmDto> armDtoList)
        {
            _arms = armDtoList.Select(Arm.FromDto).ToList();
            return this;
        }

        private static void AssertLimitOfArmsNotReached(int nbArm)
        {
            if (nbArm >= MaximumArmCount)
            {
                throw new DroidTooManyArmsException();
            }
        }

        private static void AssertDroidNotNull(IDroidDto droid)
        {
            if (droid == null)
            {
                throw new DroidNotFoundException();
            }
        }

        public static void AssertIsValid(IDroidValidationInfo validationInfo)
        {
            if(validationInfo == null)
            {
                throw new ArgumentNullException(nameof(validationInfo));
            }

            AssertLimitOfArmsNotReached(validationInfo.NbArm);
        }

        public static void AssertExists(IDroidUnicityValidationInfo validationInfo)
        {
            if (validationInfo == null)
            {
                throw new ArgumentNullException(nameof(validationInfo));
            }

            AssertDroidNotNull(validationInfo.Droid);
        }

        private const int MaximumArmCount = 2;

        private List<Arm> _arms;
    }
}
