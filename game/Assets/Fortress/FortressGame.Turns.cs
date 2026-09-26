namespace MiniFortress
{
    public sealed partial class FortressGame
    {
        int EnemiesAlive()
        {
            int count = 0; for (int i = 1; i < fighters.Count; i++) if (fighters[i].hp > 0) count++; return count;
        }
        void ResolveShot()
        {
            burst.gameObject.SetActive(false);
            if (fighters[0].hp <= 0) { Finish(false); return; }
            if (EnemiesAlive() == 0) { Finish(true); return; }
            if (current == 0)
            {
                phase = Phase.Aim;
                message += " · 남은 이동 후 '턴 넘기기'를 누르세요.";
            }
            else if (!TryEnemyRepositionAfterAttack()) NextTurn();
        }
        void EndPlayerTurn()
        {
            // A shot, landing, or exhausted movement never ends the player's turn automatically.
            if (phase != Phase.Aim || current != 0 || !grounded) return;
            mouseMove = 0;
            NextTurn();
        }
        void NextTurn()
        {
            burst.gameObject.SetActive(false);
            if (fighters[0].hp <= 0) { Finish(false); return; }
            if (EnemiesAlive() == 0) { Finish(true); return; }
            do { current++; } while (current < fighters.Count && fighters[current].hp <= 0);
            if (current >= fighters.Count)
            {
                current = 0; round++; phase = Phase.Aim; moveRemaining = MoveLimit; playerHasAttacked = false;
                message = "내 턴 · 이동하고 조준한 뒤 " + AttackName + "!";
            }
            else PlanEnemyTurn();
        }
    }
}
