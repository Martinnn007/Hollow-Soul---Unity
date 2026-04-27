using Hollow.Persistence;
using UnityEngine;

namespace Hollow.Rewards
{
    public sealed class RunCurrencyWallet
    {
        public int RunSouls { get; private set; }

        public int RunCoins { get; private set; }

        public void AddSouls(int amount)
        {
            RunSouls += Mathf.Max(0, amount);
        }

        public void AddCoins(int amount)
        {
            RunCoins += Mathf.Max(0, amount);
        }

        public bool SpendSouls(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (RunSouls < amount)
            {
                return false;
            }

            RunSouls -= amount;
            return true;
        }

        public bool SpendCoins(int amount)
        {
            if (amount <= 0)
            {
                return true;
            }

            if (RunCoins < amount)
            {
                return false;
            }

            RunCoins -= amount;
            return true;
        }

        public RunCurrencyWalletSaveState ToSaveState()
        {
            return new RunCurrencyWalletSaveState
            {
                runSouls = RunSouls,
                runCoins = RunCoins
            };
        }

        public static RunCurrencyWallet FromSaveState(RunCurrencyWalletSaveState saveState)
        {
            var wallet = new RunCurrencyWallet();
            if (saveState == null)
            {
                return wallet;
            }

            wallet.RunSouls = Mathf.Max(0, saveState.runSouls);
            wallet.RunCoins = Mathf.Max(0, saveState.runCoins);
            return wallet;
        }

    }
}
