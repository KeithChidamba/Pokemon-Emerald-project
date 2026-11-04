// =====================================================================================
// SEMI-INVULNERABLE / TURN HANDLING FIXES
// Not a real file: every method below belongs to the class named in the comment above it.
// "REPLACES" = swap out your existing method. "NEW" = add it.
// =====================================================================================


// -------------------------------------------------------------------------------------
// BELONGS TO: SemiInvulnerabilityData  (SemiInvulnerabilityData.cs)
// REPLACES:   IsInvulnerableTo(Move)
// FIX #5:     no longer writes to the shared Move ScriptableObject. The sure-hit
//             override is returned through the out parameter instead.
//             Callers: use `out var isSureHit` and use that instead of move.isSureHit.
// -------------------------------------------------------------------------------------


// -------------------------------------------------------------------------------------
// BELONGS TO: MoveLogicHandler  (MoveLogicHandler.cs)
// REPLACES:   ExecuteSemiInvulnerableMove
// FIX #5:     removed `move.isSureHit = false` (it mutated the shared Move asset)
// FIX #2:     list is cleared before AddRange so entries can't stack, and the hit turn
//             now does a full ResetState() instead of only clearing executionTurn
// -------------------------------------------------------------------------------------


// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// NEW:        helper, true when the participant currently being handled is the last one
//             to act this round (shared by SaveTurn and the auto-advance logic)
// -------------------------------------------------------------------------------------

// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// NEW:        helper for participants that don't get input (semi-invulnerable or
//             non-execution cooldown turns)
// FIX #1/#8:  if they are the last to act, execution must start from here, otherwise
//             NextTurn() wraps back to index 0 and the player is re-prompted
// -------------------------------------------------------------------------------------

// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// NEW:        helper that fully clears a participant's semi-invulnerable state
// FIX #2:     used on every early-exit path of ExecuteMoves
// -------------------------------------------------------------------------------------

// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// REPLACES:   SaveTurn
// CHANGE:     uses IsLastToAct() so the "last participant" rule lives in one place
// -------------------------------------------------------------------------------------


// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// REPLACES:   the public RemoveTurn() (NOT the private RemoveTurn(int index))
// FIX #3:     pre-added semi-invulnerable turns sit at the front of _turnHistory, so
//             list position != participant index. Find the previous participant's turn
//             by attackerKey instead of by position.
// NOTE:       relies on BattleParticipantKey values matching the turn index
//             (Player=0, PlayerPartner=1, Enemy=2, EnemyPartner=3), which they do.
// -------------------------------------------------------------------------------------


// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// REPLACES:   CheckAttackSuccess
// FIX #4:     the charge turn of a semi-invulnerable move always succeeds: no victim
//             invulnerability check and no accuracy roll. Both happen on the hit turn.
// FIX #5:     sure-hit override comes from IsInvulnerableTo's out param, not from
//             mutating the Move asset
// -------------------------------------------------------------------------------------



// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// REPLACES:   ExecuteMoves
// FIX #2:     every early exit now clears semi-invulnerable state (no stale executionTurn)
// FIX #6:     Struggle can no longer hit a semi-invulnerable target
// FIX #7:     stale/fainted slots are cleaned up and the pre-add loop uses a fresh
//             participant list instead of the one captured before status/weather damage
// CHANGE:     iterates a snapshot of _turnHistory; previousMoveData accessed null-safely
// -------------------------------------------------------------------------------------



// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// REPLACES:   ResolveTurnStateForParticipants
// FIX #1:     semi-invulnerable branch now always `yield break`s. Previously, when the
//             last participant was semi-invulnerable it started execution and then fell
//             through into the AI/player input (PP loss, wild flee, trainer actions).
// FIX #8:     non-execution cooldown turns use the same last-to-act handling, so an
//             enemy recharging in singles no longer wraps back to the player's prompt.
// -------------------------------------------------------------------------------------



// =====================================================================================
// MULTI-TARGET FIX (Earthquake / Magnitude / any isMultiTarget move)
//
// WHY IT FAILED: BattleHandler auto-selects the first active enemy as the "victim" of a
// multi-target move. CheckAttackSuccess then judged the WHOLE move against that one
// enemy. If it was in the air, Earthquake wasn't in its Fly list, so IsInvulnerableTo
// said "invulnerable", the move failed, and ApplyMultiTargetDamage never ran. The Dig
// user was never even checked, which is why the "matches" log never printed.
// The reverse was also true: once past that gate, ApplyMultiTargetDamage hit everyone
// with no per-target invulnerability check at all.
// =====================================================================================



// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// NEW:        per-target semi-invulnerability check, used by CheckAttackSuccess and by
//             MoveLogicHandler.ApplyMultiTargetDamage. No side effects (no messages, no
//             writes to the Move asset); isSureHit carries the data override.
// -------------------------------------------------------------------------------------


// -------------------------------------------------------------------------------------
// BELONGS TO: BattleParticipant  (BattleParticipant.cs)
// NEW:        the actual reachability check. Lives on the participant so that
//             TurnBasedCombatHandler, MoveLogicHandler and MoveSequenceHandler can all
//             use it without needing a reference to each other.
// -------------------------------------------------------------------------------------


// -------------------------------------------------------------------------------------
// BELONGS TO: TurnBasedCombatHandler
// REPLACES:   MoveSuccessful(Turn turn)
// CHANGE:     accuracy is rolled against an explicit target, not turn.victimKey
//             (which is arbitrary for multi-target moves). Keep a Turn overload if
//             other code calls MoveSuccessful(turn):
//                 private bool MoveSuccessful(Turn turn) => MoveSuccessful(turn.move,
//                     _battleHandler.GetParticipant(turn.attackerKey),
//                     _battleHandler.GetParticipant(turn.victimKey));
// -------------------------------------------------------------------------------------


// -------------------------------------------------------------------------------------
// BELONGS TO: MoveLogicHandler
// REPLACES:   ApplyMultiTargetDamage
// FIX:        each target is checked on its own, so Earthquake skips the Fly user and
//             still hits the Dig user (with its damage multiplier). Also covers
//             Magnitude, which calls this through MoveLogicDatabase.
// -------------------------------------------------------------------------------------




// =====================================================================================
// PIPELINE MULTI-TARGET FIX (status / stat-change moves with isMultiTarget)
//
// These loop over attacker.currentEnemies with no semi-invulnerability check, so a
// Growl-style move would still land on a flier / digger. The loops also included
// inactive (fainted, unreplaced) slots. Both are fixed through one shared helper.
// =====================================================================================


// -------------------------------------------------------------------------------------
// BELONGS TO: MoveSequenceHandler
// NEW:        the enemies a pipeline multi-target move can actually affect. Skips
//             fainted / inactive slots, drops semi-invulnerable targets the move can't
//             reach (showing their "avoided" message once), and works on a copy of the
//             list so nothing mutating currentEnemies mid-move can break the loop.
// -------------------------------------------------------------------------------------




// -------------------------------------------------------------------------------------
// BELONGS TO: MoveSequenceHandler
// REPLACES:   CheckVictimVulnerabilityToStatus
// FIX:        multi-target moves skip the single-victim guards (the "victim" is just an
//             arbitrary pick, so its existing status / protection must not block the
//             other enemies) and evaluate each reachable enemy on its own.
//             Single-target behaviour is unchanged.
// NOTE:       the status chance is still rolled once for all targets, as before.
// -------------------------------------------------------------------------------------



// -------------------------------------------------------------------------------------
// BELONGS TO: MoveSequenceHandler
// REPLACES:   HandleStatChangeApplication
// FIX:        reachable targets are worked out once (so the "avoided" message isn't
//             repeated for every stat in buffOrDebuffData), then each is changed.
// -------------------------------------------------------------------------------------


// -------------------------------------------------------------------------------------
// BELONGS TO: MoveLogicHandler
// REPLACES:   HandleMultiTargetDamage
// CHANGE:     AllEnemies used to hand ApplyMultiTargetDamage the live attacker.currentEnemies
//             list; a copy is used so the list can't change under the foreach while
//             faints / deactivations are processed mid-move (relevant for Surf).
// -------------------------------------------------------------------------------------

