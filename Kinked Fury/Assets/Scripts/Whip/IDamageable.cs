namespace KinkedFury.Whip
{
    /// <summary>Anything the whip can hurt. Put on the same object as (or a parent of) the target's collider.</summary>
    public interface IDamageable
    {
        void TakeHit(float damage);
    }
}
