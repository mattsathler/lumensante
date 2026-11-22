using System;
using System.Collections.Generic;
using System.Linq;

namespace lumen_sante.Gameplay
{
    internal sealed class WorkerReservations
    {
        private readonly Dictionary<string, string> owners = new Dictionary<string, string>(StringComparer.Ordinal);

        internal bool TryAcquire(string owner, params string[] resources)
        {
            if (string.IsNullOrWhiteSpace(owner) || resources.Any(string.IsNullOrWhiteSpace))
                throw new ArgumentException("Reserva sem profissional ou recurso.");
            if (resources.Any(resource => owners.ContainsKey(resource) && owners[resource] != owner)) return false;
            foreach (string resource in resources) owners[resource] = owner;
            return true;
        }

        internal bool IsReserved(string resource) => owners.ContainsKey(resource);

        internal void Release(string owner)
        {
            foreach (string resource in owners.Where(pair => pair.Value == owner).Select(pair => pair.Key).ToArray())
                owners.Remove(resource);
        }
    }
}
