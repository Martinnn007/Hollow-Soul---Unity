namespace Hollow.Combat
{
    public interface IIncomingDamageModifier
    {
        int ModifyIncomingDamage(DamageRequest request, int currentAmount);
    }
}
