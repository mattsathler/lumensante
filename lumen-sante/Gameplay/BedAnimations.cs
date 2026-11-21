namespace lumen_sante.Gameplay
{
    internal static class BedAnimations
    {
        internal static readonly (string Name, string Dictionary, string Clip) Lounger =
            ("Deitado — espreguiçadeira", "switch@trevor@annoys_sunbathers", "trev_annoys_sunbathers_loop_girl");

        internal static readonly (string Name, string Dictionary, string Clip)[] Poses =
        {
            ("Deitado — repouso", "amb@lo_res_idles@", "world_human_bum_slumped_right_lo_res_base"),
            ("Deitado — lado esquerdo", "amb@world_human_bum_slumped@male@laying_on_left_side@idle_a", "idle_b"),
            Lounger
        };
    }
}
