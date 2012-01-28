﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SamuraiServer.Data.Providers
{
    public class CommandResult
    {
        public IEnumerable<Unit> Units { get; set; }
        public IEnumerable<ValidationResult> Errors { get; set; }
    }

    public class CommandProcessor
    {
        private readonly GameState _match;

        public CommandProcessor(GameState match)
        {
            _match = match;
        }

        public CommandResult Process(IEnumerable<dynamic> commands)
        {
            var units = new List<Unit>();
            var errors = new List<ValidationResult>();

            if (commands == null)
                return new CommandResult { Units = units, Errors = errors };

            ValidationResult<Unit> result;

            foreach (var c in commands)
            {
                if (c.action == "move")
                {
                    result = ProcessMove(c);
                    if (result.IsValid == true)
                        units.Add(result.Data);
                    else
                    {
                        // TODO: more specific error messages?
                        errors.Add(new ValidationResult { IsValid = false, Message = result.Message });
                    }
                }

                if (c.action == "attack")
                {
                    result = ProcessAttack(c);
                    if (result.IsValid == true)
                        units.Add(result.Data);
                    else
                    {
                        // TODO: more specific error messages?
                        errors.Add(new ValidationResult { IsValid = false, Message = result.Message });
                    }
                }
                //if (c.type == "create")
                //{
                //    ProcessCreate(c);
                //}
            }

            return new CommandResult { Units = units, Errors = errors };
        }

        private ValidationResult<Unit> ProcessAttack(dynamic command)
        {
            string unitId = command.unitId.ToString();
            Guid id;
            if (!Guid.TryParse(unitId, out id))
                return null;

            int x = command.X;
            int y = command.Y;

            return AttackUnit(id, x, y);
        }

        private ValidationResult<Unit> ProcessMove(dynamic o)
        {
            string unitId = o.unitId.ToString();
            Guid id;
            if (!Guid.TryParse(unitId, out id))
                return null;

            int x = o.X;
            int y = o.Y;

            return MoveUnit(id, x, y);
        }

        private ValidationResult<Unit> MoveUnit(Guid id, int x, int y)
        {
            var foundUnit = _match.Players.SelectMany(c => c.Units).FirstOrDefault(g => g.Id == id);

            if (foundUnit != null)
            {
                if (!IsCurrentPlayer(foundUnit))
                    return ValidationResult<Unit>.Failure("This player does not have the right to move");

                var newCoordinates = new Point(x, y);
                var currentCoordinates = new Point(foundUnit.X, foundUnit.Y);
                var distance = newCoordinates.DistanceFrom(currentCoordinates);

                if (distance > foundUnit.Range)
                    return ValidationResult<Unit>.Failure("Unit cannot move that distance");

                foundUnit.X = x;
                foundUnit.Y = y;
            }

            return ValidationResult<Unit>.Success.WithData(foundUnit);
        }

        private ValidationResult<Unit> AttackUnit(Guid id, int x, int y)
        {
            var allUnits = _match.Players.SelectMany(p => p.Units).ToList();

            var attackUnit = allUnits.FirstOrDefault(u => u.Id == id);
            var targetUnit = allUnits.FirstOrDefault(u => u.X == x && u.Y == y);

            if (attackUnit == null)
                return ValidationResult<Unit>.Failure("Could not find attack unit");

            if (targetUnit == null)
                return ValidationResult<Unit>.Failure("No unit found at this location");

            // TODO: check target is within range of attacker
            // TODO: execute damage on target

            return ValidationResult<Unit>.Success.WithData(attackUnit);
        }

        public void Attack(Guid id, Guid targetId)
        {

        }

        private bool IsCurrentPlayer(Unit foundUnit)
        {
            var playerOwningUnit = _match.Players.FirstOrDefault(c => c.Units.Contains(foundUnit));
            if (playerOwningUnit == null)
                return false;

            return _match.Players[_match.Turn] == playerOwningUnit;
        }
    }
}

