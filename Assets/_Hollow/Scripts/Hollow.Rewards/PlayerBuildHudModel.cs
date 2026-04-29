namespace Hollow.Rewards
{
    public readonly struct PlayerBuildHudModel
    {
        public PlayerBuildHudModel(
            string characterName,
            int currentHealth,
            int maxHealth,
            int defense,
            bool isGuarding,
            float speed,
            int strength,
            float currentStamina,
            float maxStamina,
            float staminaRegen,
            int meleeDamage,
            int rangedDamage,
            float meleeRangeBonusMeters,
            float rangedRangeBonusMeters,
            float cooldownMultiplier,
            int coins,
            int souls,
            string activeWeapon,
            string meleeWeapon,
            string rangedWeapon,
            string armor,
            string activeItem,
            string card,
            string activeSet)
        {
            CharacterName = characterName ?? "Unknown";
            CurrentHealth = currentHealth;
            MaxHealth = maxHealth;
            Defense = defense;
            IsGuarding = isGuarding;
            Speed = speed;
            Strength = strength;
            CurrentStamina = currentStamina;
            MaxStamina = maxStamina;
            StaminaRegen = staminaRegen;
            MeleeDamage = meleeDamage;
            RangedDamage = rangedDamage;
            MeleeRangeBonusMeters = meleeRangeBonusMeters;
            RangedRangeBonusMeters = rangedRangeBonusMeters;
            CooldownMultiplier = cooldownMultiplier;
            Coins = coins;
            Souls = souls;
            ActiveWeapon = activeWeapon ?? "None";
            MeleeWeapon = meleeWeapon ?? "None";
            RangedWeapon = rangedWeapon ?? "None";
            Armor = armor ?? "None";
            ActiveItem = activeItem ?? "None";
            Card = card ?? "None";
            ActiveSet = activeSet ?? "None";
        }

        public string CharacterName { get; }
        public int CurrentHealth { get; }
        public int MaxHealth { get; }
        public int Defense { get; }
        public bool IsGuarding { get; }
        public float Speed { get; }
        public int Strength { get; }
        public float CurrentStamina { get; }
        public float MaxStamina { get; }
        public float StaminaRegen { get; }
        public int MeleeDamage { get; }
        public int RangedDamage { get; }
        public float MeleeRangeBonusMeters { get; }
        public float RangedRangeBonusMeters { get; }
        public float CooldownMultiplier { get; }
        public int Coins { get; }
        public int Souls { get; }
        public string ActiveWeapon { get; }
        public string MeleeWeapon { get; }
        public string RangedWeapon { get; }
        public string Armor { get; }
        public string ActiveItem { get; }
        public string Card { get; }
        public string ActiveSet { get; }

        public string BodyText =>
            $"BUILD\n" +
            $"Character: {CharacterName}\n" +
            $"HP: {CurrentHealth}/{MaxHealth}  DEF: {Defense}{(IsGuarding ? " Guard" : string.Empty)}\n" +
            $"Speed: {Speed:0.0}  STR: {Strength}\n" +
            $"Stamina: {CurrentStamina:0}/{MaxStamina:0} (+{StaminaRegen:0}/s)\n" +
            $"Melee: +{MeleeDamage}  Ranged: +{RangedDamage}\n" +
            $"Range: M +{MeleeRangeBonusMeters:0.##}m  R +{RangedRangeBonusMeters:0.##}m\n" +
            $"Cooldown: x{CooldownMultiplier:0.##}\n" +
            $"Coins: {Coins}  Souls: {Souls}\n\n" +
            $"Active: {ActiveWeapon}\n" +
            $"Melee: {MeleeWeapon}\n" +
            $"Ranged: {RangedWeapon}\n" +
            $"Armor: {Armor}\n" +
            $"Item: {ActiveItem}\n" +
            $"Card: {Card}\n" +
            $"Set: {ActiveSet}";
    }
}
