// Blocca accoppiamento tra:
//   - genitore e figlio (A è genitore di B, o viceversa)
//   - fratelli/sorelle diretti (A e B condividono almeno un genitore)
//
// Non blocca cugini o gradi superiori: il tracciamento si limita a
// parentAId/parentBId (profondità 1). Bilanciamento tra realismo
// biologico e complessità computazionale.
//
// AGGIUNTO: CreateOffspring imposta parentAId/parentBId sulla prole.

using UnityEngine;

public static class ReproductionSystem
{
    public static GeneticProfile TryMate(AnimalState a, AnimalState b, SimulationSettings s)
    {
        if (!a.CanMate(s) || !b.CanMate(s)) return null;
        if (a.species != b.species) return null;

        float dist = Vector2.Distance(a.position, b.position);
        if (dist > a.matingRange && dist > b.matingRange) return null;

        // ── Blocco incesto di primo grado ─────────────────────────────────────
        if (AreCloselyRelated(a, b)) return null;

        var childGenes = GeneticsOps.Reproduce(a.genes, b.genes, s);

        float cost = s.offspringEnergyFraction * 0.5f;
        a.energy = Mathf.Max(0f, a.energy - cost);
        b.energy = Mathf.Max(0f, b.energy - cost);

        a.reproductionCooldown = s.reproductionCooldown;
        b.reproductionCooldown = s.reproductionCooldown;

        a.offspringCount++;
        b.offspringCount++;

        return childGenes;
    }

    public static AnimalState CreateOffspring(
        AnimalState parentA,
        AnimalState parentB,
        GeneticProfile childGenes,
        SimulationSettings s,
        int newId)
    {
        return new AnimalState
        {
            id = newId,
            species = parentA.species,
            genes = childGenes,
            energy = s.offspringEnergyFraction * 1.5f,
            hunger = 0f,
            thirst = 0f,
            position = parentA.position + Random.insideUnitCircle * 0.5f,
            velocity = Vector2.zero,
            age = 0f,
            reproductionCooldown = s.reproductionCooldown,
            // ── Tracciamento genealogico ───────────────────────────────────
            parentAId = parentA.id,
            parentBId = parentB.id,
        };
    }

    // ── Controllo consanguineità ──────────────────────────────────────────────

    /// <summary>
    /// Restituisce true se A e B sono parenti di primo grado:
    ///   - Genitore-figlio: uno è genitore diretto dell'altro
    ///   - Fratelli: condividono almeno un genitore tracciato
    ///
    /// Se uno dei due non ha genitori tracciati (parentAId == -1 e parentBId == -1),
    /// il controllo viene saltato: prima generazione può accoppiarsi liberamente.
    /// </summary>
    private static bool AreCloselyRelated(AnimalState a, AnimalState b)
    {
        bool aHasParents = a.parentAId >= 0 || a.parentBId >= 0;
        bool bHasParents = b.parentAId >= 0 || b.parentBId >= 0;

        // Se nessuno dei due ha storia genealogica, non possiamo sapere:
        // permettiamo l'accoppiamento (prima generazione)
        if (!aHasParents && !bHasParents) return false;

        // ── Genitore-figlio ───────────────────────────────────────────────────
        // A è genitore di B?
        if (b.parentAId == a.id || b.parentBId == a.id) return true;
        // B è genitore di A?
        if (a.parentAId == b.id || a.parentBId == b.id) return true;

        // ── Fratelli (condividono almeno un genitore) ─────────────────────────
        if (aHasParents && bHasParents)
        {
            if (a.parentAId >= 0 && (a.parentAId == b.parentAId || a.parentAId == b.parentBId))
                return true;
            if (a.parentBId >= 0 && (a.parentBId == b.parentAId || a.parentBId == b.parentBId))
                return true;
        }

        return false;
    }
}