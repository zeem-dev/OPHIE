using UnityEngine;
using UnityEditor;

public static class ProceduralAnimationTemplates
{
    public enum TemplateType
    {
        MeleeSlamStrike,
        DashSprint,
        BuffShield,
        ProjectileThrow,
        CastSummon
    }

    public static void ApplyTemplate(AnimationClip clip, string abilityName, TemplateType type, float duration, float intensity, bool useRootMotion)
    {
        // Clear existing curves to start clean
        clip.ClearCurves();

        // Configure loop time based on animation type
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = (abilityName.Contains("Sprint") || abilityName.Contains("Dash") && !useRootMotion);
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        switch (type)
        {
            case TemplateType.MeleeSlamStrike:
                GenerateMeleeSlamStrike(clip, abilityName, duration, intensity, useRootMotion);
                break;
            case TemplateType.DashSprint:
                GenerateDashSprint(clip, abilityName, duration, intensity, useRootMotion);
                break;
            case TemplateType.BuffShield:
                GenerateBuffShield(clip, abilityName, duration, intensity, useRootMotion);
                break;
            case TemplateType.ProjectileThrow:
                GenerateProjectileThrow(clip, abilityName, duration, intensity, useRootMotion);
                break;
            case TemplateType.CastSummon:
                GenerateCastSummon(clip, abilityName, duration, intensity, useRootMotion);
                break;
        }
    }

    private static void AddMuscleCurve(AnimationClip clip, string propertyName, Keyframe[] keys)
    {
        AnimationCurve curve = new AnimationCurve(keys);
        clip.SetCurve("", typeof(Animator), propertyName, curve);
    }

    private static void AddRootTranslationZ(AnimationClip clip, Keyframe[] keys)
    {
        AnimationCurve curve = new AnimationCurve(keys);
        clip.SetCurve("", typeof(Animator), "RootT.z", curve);
    }

    private static void AddRootTranslationY(AnimationClip clip, Keyframe[] keys)
    {
        AnimationCurve curve = new AnimationCurve(keys);
        clip.SetCurve("", typeof(Animator), "RootT.y", curve);
    }

    private static void AddRootTranslationX(AnimationClip clip, Keyframe[] keys)
    {
        AnimationCurve curve = new AnimationCurve(keys);
        clip.SetCurve("", typeof(Animator), "RootT.x", curve);
    }

    // =========================================================================
    // 1. MELEE SLAM / STRIKE TEMPLATE
    // =========================================================================
    private static void GenerateMeleeSlamStrike(AnimationClip clip, string name, float D, float I, bool useRootMotion)
    {
        // Spine & Chest (Anticipation back, Action snap forward, Settle)
        AddMuscleCurve(clip, "Spine Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.25f * D, -0.4f * I),
            new Keyframe(0.45f * D, 0.7f * I),
            new Keyframe(0.7f * D, 0.3f * I),
            new Keyframe(D, 0f)
        });
        AddMuscleCurve(clip, "Chest Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.25f * D, -0.3f * I),
            new Keyframe(0.45f * D, 0.5f * I),
            new Keyframe(D, 0f)
        });

        // Head looking down during strike
        AddMuscleCurve(clip, "Head Nod Down-Up", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.25f * D, -0.2f * I), // look up slightly during windup
            new Keyframe(0.45f * D, 0.5f * I),  // look down at strike
            new Keyframe(D, 0f)
        });

        if (name.Contains("MetalSlam") || name.Contains("FullBodyShockwave") || name.Contains("StructuralReinforcement"))
        {
            // Two-handed overhead smash or heavy stomp slam
            AddMuscleCurve(clip, "Right Shoulder Down-Up", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.8f * I), // hands raised high
                new Keyframe(0.45f * D, -0.6f * I), // slammed down
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Left Shoulder Down-Up", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.8f * I),
                new Keyframe(0.45f * D, -0.6f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.8f * I),
                new Keyframe(0.45f * D, -0.5f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Left Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.8f * I),
                new Keyframe(0.45f * D, -0.5f * I),
                new Keyframe(D, 0f)
            });

            // Deep crouch on impact
            AddMuscleCurve(clip, "Left Upper Leg Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, -0.1f * I),
                new Keyframe(0.45f * D, 0.7f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Upper Leg Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, -0.1f * I),
                new Keyframe(0.45f * D, 0.7f * I),
                new Keyframe(D, 0f)
            });

            AddRootTranslationY(clip, new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.2f * I), // jump up slightly
                new Keyframe(0.45f * D, -0.5f * I), // slam down low
                new Keyframe(0.7f * D, -0.2f * I),
                new Keyframe(D, 0f)
            });
        }
        else // ArcSlash, ConcussiveBlast, GermaneToss, etc.
        {
            // Right-arm swing slash or toss
            AddMuscleCurve(clip, "Right Shoulder Down-Up", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.2f * I),
                new Keyframe(0.45f * D, -0.3f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, -0.8f * I), // pull right hand back
                new Keyframe(0.45f * D, 0.9f * I),  // slash forward
                new Keyframe(0.7f * D, 0.4f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Arm Down-Up", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.2f * I),
                new Keyframe(0.45f * D, 0.4f * I),
                new Keyframe(D, 0f)
            });
            // Left arm is counter-balancing
            AddMuscleCurve(clip, "Left Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.5f * I),
                new Keyframe(0.45f * D, -0.6f * I),
                new Keyframe(D, 0f)
            });

            // Rotational twist for slash
            AddMuscleCurve(clip, "Spine Twist", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, -0.4f * I), // wind up twist
                new Keyframe(0.45f * D, 0.6f * I),  // follow through
                new Keyframe(D, 0f)
            });

            if (useRootMotion)
            {
                AddRootTranslationZ(clip, new Keyframe[] {
                    new Keyframe(0f, 0f),
                    new Keyframe(0.25f * D, 0f),
                    new Keyframe(0.45f * D, 1.2f * I), // step forward
                    new Keyframe(D, 1.2f * I)
                });
            }
        }
    }

    // =========================================================================
    // 2. DASH / SPRINT TEMPLATE
    // =========================================================================
    private static void GenerateDashSprint(AnimationClip clip, string name, float D, float I, bool useRootMotion)
    {
        // Continuous running or rapid jet forward
        // Body tilts way forward, knees high
        AddMuscleCurve(clip, "Spine Front-Back", new Keyframe[] {
            new Keyframe(0f, 0.3f * I),
            new Keyframe(0.25f * D, 0.6f * I),
            new Keyframe(0.5f * D, 0.5f * I),
            new Keyframe(0.75f * D, 0.6f * I),
            new Keyframe(D, 0.3f * I)
        });

        AddMuscleCurve(clip, "Neck Nod Down-Up", new Keyframe[] {
            new Keyframe(0f, -0.2f * I), // head looks forward/up while leaning
            new Keyframe(0.5f * D, -0.3f * I),
            new Keyframe(D, -0.2f * I)
        });

        // Alternating leg lift/push
        AddMuscleCurve(clip, "Left Upper Leg Front-Back", new Keyframe[] {
            new Keyframe(0f, 0.4f * I),
            new Keyframe(0.25f * D, -0.3f * I),
            new Keyframe(0.5f * D, 0.5f * I),
            new Keyframe(0.75f * D, -0.2f * I),
            new Keyframe(D, 0.4f * I)
        });
        AddMuscleCurve(clip, "Right Upper Leg Front-Back", new Keyframe[] {
            new Keyframe(0f, -0.3f * I),
            new Keyframe(0.25f * D, 0.5f * I),
            new Keyframe(0.5f * D, -0.2f * I),
            new Keyframe(0.75f * D, 0.4f * I),
            new Keyframe(D, -0.3f * I)
        });

        AddMuscleCurve(clip, "Left Lower Leg Stretch", new Keyframe[] {
            new Keyframe(0f, -0.6f),
            new Keyframe(0.5f * D, -0.1f),
            new Keyframe(D, -0.6f)
        });
        AddMuscleCurve(clip, "Right Lower Leg Stretch", new Keyframe[] {
            new Keyframe(0f, -0.1f),
            new Keyframe(0.5f * D, -0.6f),
            new Keyframe(D, -0.1f)
        });

        // Arms pumping
        AddMuscleCurve(clip, "Right Arm Front-Back", new Keyframe[] {
            new Keyframe(0f, 0.5f * I),
            new Keyframe(0.5f * D, -0.5f * I),
            new Keyframe(D, 0.5f * I)
        });
        AddMuscleCurve(clip, "Left Arm Front-Back", new Keyframe[] {
            new Keyframe(0f, -0.5f * I),
            new Keyframe(0.5f * D, 0.5f * I),
            new Keyframe(D, -0.5f * I)
        });

        if (useRootMotion)
        {
            // Rapid translation forward
            float dashDistance = name.Contains("Dash") ? 6f * I : 4f * I;
            AddRootTranslationZ(clip, new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.2f * D, dashDistance * 0.1f),
                new Keyframe(0.5f * D, dashDistance * 0.6f),
                new Keyframe(D, dashDistance)
            });
        }
    }

    // =========================================================================
    // 3. BUFF / SHIELD TEMPLATE
    // =========================================================================
    private static void GenerateBuffShield(AnimationClip clip, string name, float D, float I, bool useRootMotion)
    {
        // Grounded, powerful, chest open pose
        AddMuscleCurve(clip, "Spine Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.2f * D, -0.2f * I), // bend down slightly to load
            new Keyframe(0.4f * D, -0.5f * I), // chest out, spine back
            new Keyframe(0.8f * D, -0.4f * I), // holding pose
            new Keyframe(D, 0f)
        });
        AddMuscleCurve(clip, "Chest Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.4f * D, -0.4f * I),
            new Keyframe(D, 0f)
        });

        if (name.Contains("HeatShield") || name.Contains("EnergyRedirect") || name.Contains("SporeArmor"))
        {
            // Hold one arm in front as shield, other arm back for stability
            AddMuscleCurve(clip, "Left Shoulder Down-Up", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, 0.6f * I), // left arm up
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Left Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, 0.7f * I), // hold forward
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Left Forearm Stretch", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, -0.3f), // forearm slightly bent for defense
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, -0.5f * I), // right arm back
                new Keyframe(D, 0f)
            });
        }
        else // TotalMetalization, FullPlating, OverchargeRelease, StructuralReinforcement
        {
            // Double arm flex / roar stance
            AddMuscleCurve(clip, "Right Shoulder Down-Up", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, 0.4f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Left Shoulder Down-Up", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, 0.4f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, 0.5f * I), // flex arms out to sides
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Left Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, 0.5f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Forearm Stretch", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, -0.6f * I), // bent elbow flex
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Left Forearm Stretch", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, -0.6f * I),
                new Keyframe(D, 0f)
            });

            // Head tilting back in power roar
            AddMuscleCurve(clip, "Head Nod Down-Up", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, -0.6f * I), // head back
                new Keyframe(D, 0f)
            });
        }

        // Crouch legs slightly for grounded feel
        AddMuscleCurve(clip, "Left Upper Leg Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.3f * D, 0.3f * I),
            new Keyframe(D, 0f)
        });
        AddMuscleCurve(clip, "Right Upper Leg Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.3f * D, 0.3f * I),
            new Keyframe(D, 0f)
        });
        AddRootTranslationY(clip, new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.3f * D, -0.2f * I),
            new Keyframe(D, 0f)
        });
    }

    // =========================================================================
    // 4. PROJECTILE / THROW TEMPLATE
    // =========================================================================
    private static void GenerateProjectileThrow(AnimationClip clip, string name, float D, float I, bool useRootMotion)
    {
        // Pull back arm, then launch forward
        AddMuscleCurve(clip, "Spine Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.25f * D, -0.3f * I), // lean back
            new Keyframe(0.4f * D, 0.5f * I),  // throw forward
            new Keyframe(D, 0f)
        });

        if (name.Contains("FireBarrage") || name.Contains("PrecisionBurst"))
        {
            // Rapid shooting/channeling pose: alternate rapid arm punches
            AddMuscleCurve(clip, "Right Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0.3f * I),
                new Keyframe(0.2f * D, 0.9f * I),
                new Keyframe(0.4f * D, -0.3f * I),
                new Keyframe(0.6f * D, 0.9f * I),
                new Keyframe(0.8f * D, -0.3f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Left Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.1f * D, -0.3f * I),
                new Keyframe(0.3f * D, 0.9f * I),
                new Keyframe(0.5f * D, -0.3f * I),
                new Keyframe(0.7f * D, 0.9f * I),
                new Keyframe(D, 0f)
            });
        }
        else // SporeBall, PrecisionBurst standard, FlameBurst standard, ConcussiveBlast
        {
            // Standard single hand projectile throw
            AddMuscleCurve(clip, "Right Shoulder Down-Up", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.3f * I),
                new Keyframe(0.4f * D, -0.2f * I),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, -0.7f * I), // pull right arm back
                new Keyframe(0.4f * D, 0.9f * I),  // throw right arm forward
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Forearm Stretch", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, -0.6f * I), // bend elbow
                new Keyframe(0.4f * D, 0.8f * I),  // extend fully
                new Keyframe(D, 0f)
            });
            // Left arm holds pose for balance
            AddMuscleCurve(clip, "Left Arm Front-Back", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.25f * D, 0.4f * I),
                new Keyframe(0.4f * D, -0.5f * I),
                new Keyframe(D, 0f)
            });
        }
    }

    // =========================================================================
    // 5. CAST / SUMMON / OVERLOAD TEMPLATE
    // =========================================================================
    private static void GenerateCastSummon(AnimationClip clip, string name, float D, float I, bool useRootMotion)
    {
        // Lift hands up, charge up body, release energy
        AddMuscleCurve(clip, "Spine Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.3f * D, -0.4f * I), // arch back
            new Keyframe(0.6f * D, 0.4f * I),  // cast forward/down
            new Keyframe(D, 0f)
        });

        // Lift both arms high
        AddMuscleCurve(clip, "Right Shoulder Down-Up", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.3f * D, 0.9f * I), // hands way up
            new Keyframe(0.6f * D, 0.2f * I), // bring forward
            new Keyframe(D, 0f)
        });
        AddMuscleCurve(clip, "Left Shoulder Down-Up", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.3f * D, 0.9f * I),
            new Keyframe(0.6f * D, 0.2f * I),
            new Keyframe(D, 0f)
        });

        AddMuscleCurve(clip, "Right Arm Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.3f * D, 0.6f * I),
            new Keyframe(0.6f * D, 0.8f * I),
            new Keyframe(D, 0f)
        });
        AddMuscleCurve(clip, "Left Arm Front-Back", new Keyframe[] {
            new Keyframe(0f, 0f),
            new Keyframe(0.3f * D, 0.6f * I),
            new Keyframe(0.6f * D, 0.8f * I),
            new Keyframe(D, 0f)
        });

        if (name.Contains("TotalOverload") || name.Contains("Discharge") || name.Contains("NeuralSurge"))
        {
            // Float/levitate pose
            AddRootTranslationY(clip, new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, 0.6f * I), // hover
                new Keyframe(0.8f * D, 0.5f * I),
                new Keyframe(D, 0f)
            });

            // Twiddle limbs/hands slightly for overload jitter
            AddMuscleCurve(clip, "Left Hand Stretch", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.2f * D, 0.8f),
                new Keyframe(0.4f * D, -0.8f),
                new Keyframe(0.6f * D, 0.8f),
                new Keyframe(0.8f * D, -0.8f),
                new Keyframe(D, 0f)
            });
            AddMuscleCurve(clip, "Right Hand Stretch", new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.2f * D, -0.8f),
                new Keyframe(0.4f * D, 0.8f),
                new Keyframe(0.6f * D, -0.8f),
                new Keyframe(0.8f * D, 0.8f),
                new Keyframe(D, 0f)
            });
        }
        else // CloneSpawn, MassDetonation, SporeBall, etc.
        {
            // Ground slam / release pose
            AddRootTranslationY(clip, new Keyframe[] {
                new Keyframe(0f, 0f),
                new Keyframe(0.3f * D, 0.1f * I),
                new Keyframe(0.6f * D, -0.4f * I), // settle low
                new Keyframe(D, 0f)
            });
        }
    }
}
