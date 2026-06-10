using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(Weapon_Config))]
[CanEditMultipleObjects]
public class WeaponConfigEditor : Editor
{
    // Générales
    SerializedProperty weaponType;
    SerializedProperty armsPrefab;
    SerializedProperty hitMask;
    SerializedProperty weaponID;
    SerializedProperty animatorController;
    SerializedProperty weaponIcon;

    // Ranged
    SerializedProperty fireMode, fireRate, range, damage;
    SerializedProperty useSpread, spreadAngle, spreadMultADS;
    SerializedProperty spreadWithDistance, distanceStart, distanceEnd, maxExtraSpreadByDistance, distanceSpreadCurve;
    SerializedProperty spreadWithBloom, bloomPerShot, bloomMax, bloomRecovery, chainResetDelay;
    SerializedProperty burstCount, burstInterval, burstCooldown;
    SerializedProperty maxAmmo, reloadRate, reloadDelay;
    SerializedProperty stopShootDelay,soundNameShoot;
    SerializedProperty damageReducerMinDistance, damageReducerPerMeter, damageReducerCap;

    // Bomb / Mine
    SerializedProperty throwableType;
    SerializedProperty bombPrefab;
    SerializedProperty minePrefab;
    SerializedProperty explosionRadius, explosionDamage, fuseTime;
    SerializedProperty arcHeight, power, flatSpeed;
    
    // Heal
    SerializedProperty healAmount, healCharges, bigHealSprite;

    private void OnEnable()
    {
        weaponType = serializedObject.FindProperty("weaponType");
        armsPrefab = serializedObject.FindProperty("armsPrefab");
        hitMask = serializedObject.FindProperty("hitMask");
        weaponID = serializedObject.FindProperty("weaponID");
        animatorController = serializedObject.FindProperty("animatorController");
        weaponIcon = serializedObject.FindProperty("weaponIcon");

        fireMode = serializedObject.FindProperty("fireMode");
        fireRate = serializedObject.FindProperty("fireRate");
        range = serializedObject.FindProperty("range");
        damage = serializedObject.FindProperty("damage");
        useSpread = serializedObject.FindProperty("useSpread");
        spreadAngle = serializedObject.FindProperty("spreadAngle");
        spreadMultADS = serializedObject.FindProperty("spreadMultADS");

        spreadWithDistance = serializedObject.FindProperty("spreadWithDistance");
        distanceStart = serializedObject.FindProperty("distanceStart");
        distanceEnd = serializedObject.FindProperty("distanceEnd");
        maxExtraSpreadByDistance = serializedObject.FindProperty("maxExtraSpreadByDistance");
        distanceSpreadCurve = serializedObject.FindProperty("distanceSpreadCurve");

        spreadWithBloom = serializedObject.FindProperty("spreadWithBloom");
        bloomPerShot = serializedObject.FindProperty("bloomPerShot");
        bloomMax = serializedObject.FindProperty("bloomMax");
        bloomRecovery = serializedObject.FindProperty("bloomRecovery");
        chainResetDelay = serializedObject.FindProperty("chainResetDelay");

        burstCount = serializedObject.FindProperty("burstCount");
        burstInterval = serializedObject.FindProperty("burstInterval");
        burstCooldown = serializedObject.FindProperty("burstCooldown");
        maxAmmo = serializedObject.FindProperty("maxAmmo");
        reloadRate = serializedObject.FindProperty("reloadRate");
        reloadDelay = serializedObject.FindProperty("reloadDelay");
        stopShootDelay = serializedObject.FindProperty("stopShootDelay");
        soundNameShoot = serializedObject.FindProperty("soundNameShoot");
        damageReducerMinDistance = serializedObject.FindProperty("damageReducerMinDistance");
        damageReducerPerMeter = serializedObject.FindProperty("damageReducerPerMeter");
        damageReducerCap = serializedObject.FindProperty("damageReducerCap");

        throwableType = serializedObject.FindProperty("throwableType");
        bombPrefab = serializedObject.FindProperty("bombPrefab");
        minePrefab = serializedObject.FindProperty("minePrefab");
        explosionRadius = serializedObject.FindProperty("explosionRadius");
        explosionDamage = serializedObject.FindProperty("explosionDamage");
        fuseTime = serializedObject.FindProperty("fuseTime");
        arcHeight = serializedObject.FindProperty("arcHeight");
        power = serializedObject.FindProperty("power");
        flatSpeed = serializedObject.FindProperty("flatSpeed");

        healAmount = serializedObject.FindProperty("healAmount");
        healCharges = serializedObject.FindProperty("healCharges");
        bigHealSprite = serializedObject.FindProperty("bigHealSprite");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── GENERAL SETTINGS (Visible pour tous) ─────────────────────────────
        EditorGUILayout.LabelField("VISUAL & GENERAL SETTINGS", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(weaponType);
        EditorGUILayout.PropertyField(weaponID);
        EditorGUILayout.PropertyField(animatorController);
        EditorGUILayout.PropertyField(armsPrefab);
        EditorGUILayout.PropertyField(weaponIcon);

        EditorGUILayout.PropertyField(hitMask);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("VFX SETTINGS", EditorStyles.boldLabel);
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);

        // ── CONDITIONNEL ─────────────────────────────────────────────────────
        WeaponType type = (WeaponType)weaponType.enumValueIndex;
        switch (type)
        {
            case WeaponType.Ranged: DrawRangedSettings(); break;
            case WeaponType.Bomb: DrawBombSettings(); break;
            case WeaponType.Melee:
                EditorGUILayout.HelpBox("Configuration pour le corps à corps à implémenter...", MessageType.Info);
                break;
            case WeaponType.Heal: DrawHealSettings(); break;
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRangedSettings()
    {
        EditorGUILayout.LabelField("RANGED WEAPON SETTINGS", EditorStyles.boldLabel);

        EditorGUILayout.PropertyField(fireMode);
        EditorGUILayout.PropertyField(fireRate);
        EditorGUILayout.PropertyField(range);
        EditorGUILayout.PropertyField(damage);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Ammunition", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(maxAmmo);
        EditorGUILayout.PropertyField(reloadRate);
        EditorGUILayout.PropertyField(reloadDelay);

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Burst Settings", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(burstCount);
        EditorGUILayout.PropertyField(burstInterval);
        EditorGUILayout.PropertyField(burstCooldown);

        EditorGUILayout.Space(5);
        EditorGUILayout.PropertyField(useSpread);
        if (useSpread.boolValue)
        {
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(spreadAngle);
            EditorGUILayout.PropertyField(spreadMultADS);

            EditorGUILayout.PropertyField(spreadWithDistance);
            if (spreadWithDistance.boolValue)
            {
                EditorGUILayout.PropertyField(distanceStart);
                EditorGUILayout.PropertyField(distanceEnd);
                EditorGUILayout.PropertyField(maxExtraSpreadByDistance);
                EditorGUILayout.PropertyField(distanceSpreadCurve);
            }

            EditorGUILayout.PropertyField(spreadWithBloom);
            if (spreadWithBloom.boolValue)
            {
                EditorGUILayout.PropertyField(bloomPerShot);
                EditorGUILayout.PropertyField(bloomMax);
                EditorGUILayout.PropertyField(bloomRecovery);
                EditorGUILayout.PropertyField(chainResetDelay);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUILayout.Space(5f);
        EditorGUILayout.PropertyField(stopShootDelay);
        EditorGUILayout.Space(5f);
        EditorGUILayout.PropertyField(soundNameShoot);
        EditorGUILayout.Space(5f);
        EditorGUILayout.PropertyField(damageReducerMinDistance);
        EditorGUILayout.Space(5f);
        EditorGUILayout.PropertyField(damageReducerPerMeter);
        EditorGUILayout.Space(5f);
        EditorGUILayout.PropertyField(damageReducerCap);
    }

    private void DrawBombSettings()
    {
        EditorGUILayout.LabelField("THROWABLE SETTINGS", EditorStyles.boldLabel);

        // ── Sélecteur de type ────────────────────────────────────────────────
        EditorGUILayout.PropertyField(throwableType, new GUIContent("Throwable Type"));
        EditorGUILayout.Space(4);

        ThrowableType tType = (ThrowableType)throwableType.enumValueIndex;

        // ── Prefab conditionnel ──────────────────────────────────────────────
        if (tType == ThrowableType.Bomb)
        {
            EditorGUILayout.PropertyField(bombPrefab, new GUIContent("Bomb Prefab"));
        }
        else
        {
            EditorGUILayout.PropertyField(minePrefab, new GUIContent("Mine Prefab"));
        }

        // ── Explosion ────────────────────────────────────────────────────────
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("EXPLOSION", EditorStyles.miniBoldLabel);
        EditorGUILayout.PropertyField(explosionRadius);
        EditorGUILayout.PropertyField(explosionDamage);

        // Label du fuseTime adapté au type
        string fuseLabel = tType == ThrowableType.Mine
            ? "Arming Delay (s)"
            : "Fuse Time (s)";
        EditorGUILayout.PropertyField(fuseTime, new GUIContent(fuseLabel));

        // ── Launch ───────────────────────────────────────────────────────────
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("LAUNCH SETTINGS", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(arcHeight);
        EditorGUILayout.PropertyField(power);
        EditorGUILayout.PropertyField(flatSpeed);


        // ── Info contextuelle ─────────────────────────────────────────────────
        EditorGUILayout.Space(6);
        if (tType == ThrowableType.Mine)
            EditorGUILayout.HelpBox(
                "Mine : s'arme après le délai indiqué, puis explose au contact d'un collider dans le hitMask.",
                MessageType.Info);
        else
            EditorGUILayout.HelpBox(
                "Bombe : explose après le fuseTime ou à l'impact si 'Explode On Impact' est activé sur le prefab.",
                MessageType.Info);
    }

    private void DrawHealSettings()
    {
        EditorGUILayout.LabelField("HEAL SETTINGS", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(healAmount, new GUIContent("HP rendus par utilisation"));
        EditorGUILayout.PropertyField(healCharges, new GUIContent("Charges par pickup"));
        EditorGUILayout.HelpBox(
            "Chaque pickup ajoute 'Charges' utilisations. Chaque utilisation soigne 'healAmount' HP.",
            MessageType.Info);
        EditorGUILayout.PropertyField(bigHealSprite);

    }

}