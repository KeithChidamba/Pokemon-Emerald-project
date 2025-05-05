using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

public class Wild_pkm : MonoBehaviour
{
    public Battle_Participant pokemon_participant;
    public Battle_Participant Enemy_pokemon;
    public bool InBattle = false;
    public bool RanAway = false;
    public bool CanAttack = true;
    public static Wild_pkm instance;
    [SerializeField]private bool Used_move = false;
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;
    }
    private void Start()
    {
        Turn_Based_Combat.instance.OnNewTurn += Reset_move;
    }

    public void Can_Attack()
    {
        CanAttack = true;
    }
    void Reset_move()
    {
        Used_move = false;
    }
    private void Update()
    {
        if (!InBattle) return;
        RanAway = false;
        Make_Decision();
    }
    private void Make_Decision()
    {
        //check if its pokemon's turn
        if (Battle_handler.Instance.battleParticipants[Turn_Based_Combat.instance.Current_pkm_turn].pokemon == pokemon_participant.pokemon && !Used_move && CanAttack)
        {
            Select_player(0);//attack player, since its single battle
            if(Utility.RandomRange(1,11)>3)//70% chance
                choose_move();
            else
                RunAway();
        }
    }

    void RunAway()
    {
        if(!pokemon_participant.canEscape)
        {
            Dialogue_handler.instance.Battle_Info(pokemon_participant.pokemon.Pokemon_name +" is trapped");
            return;
        }
        Dialogue_handler.instance.Battle_Info(pokemon_participant.pokemon.Pokemon_name+" ran away");
        Battle_handler.Instance.End_Battle(false);
        InBattle = false;
        RanAway = true;
    }
    public void Select_player(int selectedIndex)
    {
        //enemy choosing player
        pokemon_participant.enemySelected = true;
        Battle_handler.Instance.currentEnemyIndex = selectedIndex;
    }
    private void choose_move()
    {
        if(Utility.RandomRange(1,11)<5)//40% chance
            Use_random();
        else
        {
            if (Utility.RandomRange(1, 11) < 8)
            {
                if (CanUse_effective());
                else
                    UseStrongest_move();
            }
            else
                UseStrongest_move();
        }
    }
    
    private void UseStrongest_move()
    {
        List<Move> strongest_move = new();
        List<Move> mock_moveset = new();
        foreach (Move m in pokemon_participant.pokemon.move_set)
        {
            if (!BattleOperations.IsImmuneTo(Enemy_pokemon.pokemon, m.type)) //look for all non-immune moves
                mock_moveset.Add(m);
        }
        if (mock_moveset.Count > 0)
        {
            strongest_move = mock_moveset.OrderByDescending(p => p.Move_damage).ToList();
            use_move(strongest_move[0]);
        }
        else
            Use_random();
    }

    void Use_random()
    {
        int rand_move = Utility.RandomRange(0, pokemon_participant.pokemon.move_set.Count);
        use_move(pokemon_participant.pokemon.move_set[rand_move]);
    }

    void use_move(Move move)
    {
        Battle_handler.Instance.UseMove(move,pokemon_participant);
        Used_move = true;
    }
    bool CanUse_effective()
    {
        foreach (Move m in pokemon_participant.pokemon.move_set)
        {//look for super effective attacking move
            float eff = BattleOperations.GetTypeEffectiveness(Enemy_pokemon, m.type);
            if ( eff > 1 && !m.is_Buff_Debuff)
            {
                use_move(m);
                return true;
            }
        }
        return false;
    }
}
