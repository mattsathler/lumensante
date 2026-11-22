using System;

namespace lumen_sante.Clinical
{
    internal sealed class CareAuthor
    {
        internal static readonly CareAuthor Player = new CareAuthor("player", "Jogador", true);

        private CareAuthor(string id, string name, bool isPlayer)
        {
            Id = id;
            Name = name;
            IsPlayer = isPlayer;
        }

        internal static CareAuthor Doctor(string id, string name)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("O Doctor precisa de ID e nome.");
            return new CareAuthor(id, name, false);
        }

        internal string Id { get; }
        internal string Name { get; }
        internal bool IsPlayer { get; }
    }

    internal sealed class CareDecision
    {
        internal CareDecision(long revision, long at, CareAuthor author, string action, string detail)
        {
            Revision = revision;
            At = at;
            Author = author;
            Action = action;
            Detail = detail;
        }

        internal long Revision { get; }
        internal long At { get; }
        internal CareAuthor Author { get; }
        internal string Action { get; }
        internal string Detail { get; }
    }
}
