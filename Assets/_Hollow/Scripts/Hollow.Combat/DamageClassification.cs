namespace Hollow.Combat
{
    public enum DamageChannel
    {
        Physical = 0,
        NonPhysical = 1,
        Elemental = 2,
        Explosion = 3,
        Environmental = 4
    }

    public enum DamageDelivery
    {
        Melee = 0,
        Projectile = 1,
        Area = 2,
        Contact = 3
    }

    public enum ImpactForceClass
    {
        Light = 0,
        Medium = 1,
        Heavy = 2,
        Massive = 3
    }

    public enum DamageElement
    {
        None = 0,
        Fire = 1,
        Water = 2,
        Cursed = 3,
        Cosmic = 4,
        Soul = 5,
        Energy = 6
    }

    public readonly struct DamageClassification
    {
        public DamageClassification(
            DamageChannel channel,
            DamageDelivery delivery,
            ImpactForceClass forceClass,
            DamageElement element = DamageElement.None)
        {
            Channel = channel;
            Delivery = delivery;
            ForceClass = forceClass;
            Element = element;
        }

        public DamageChannel Channel { get; }

        public DamageDelivery Delivery { get; }

        public ImpactForceClass ForceClass { get; }

        public DamageElement Element { get; }

        public static DamageClassification PhysicalMelee(ImpactForceClass forceClass)
        {
            return new DamageClassification(DamageChannel.Physical, DamageDelivery.Melee, forceClass);
        }

        public static DamageClassification PhysicalProjectile(ImpactForceClass forceClass)
        {
            return new DamageClassification(DamageChannel.Physical, DamageDelivery.Projectile, forceClass);
        }

        public static DamageClassification PhysicalContact(ImpactForceClass forceClass)
        {
            return new DamageClassification(DamageChannel.Physical, DamageDelivery.Contact, forceClass);
        }

        public static DamageClassification NonPhysicalMelee(ImpactForceClass forceClass, DamageElement element = DamageElement.None)
        {
            return new DamageClassification(DamageChannel.NonPhysical, DamageDelivery.Melee, forceClass, element);
        }

        public static DamageClassification NonPhysicalProjectile(ImpactForceClass forceClass, DamageElement element = DamageElement.None)
        {
            return new DamageClassification(DamageChannel.NonPhysical, DamageDelivery.Projectile, forceClass, element);
        }

        public static DamageClassification ElementalProjectile(ImpactForceClass forceClass, DamageElement element)
        {
            return new DamageClassification(DamageChannel.Elemental, DamageDelivery.Projectile, forceClass, element);
        }

        public static DamageClassification Explosion(ImpactForceClass forceClass)
        {
            return new DamageClassification(DamageChannel.Explosion, DamageDelivery.Area, forceClass);
        }

        public static DamageClassification Environmental(ImpactForceClass forceClass)
        {
            return new DamageClassification(DamageChannel.Environmental, DamageDelivery.Area, forceClass);
        }

        public static DamageClassification FromThreat(DamageThreatKind threatKind)
        {
            return threatKind switch
            {
                DamageThreatKind.Heavy => PhysicalMelee(ImpactForceClass.Heavy),
                DamageThreatKind.Boss => PhysicalContact(ImpactForceClass.Massive),
                DamageThreatKind.StrongProjectile => PhysicalProjectile(ImpactForceClass.Heavy),
                DamageThreatKind.Environmental => Environmental(ImpactForceClass.Medium),
                _ => PhysicalMelee(ImpactForceClass.Light)
            };
        }
    }
}
