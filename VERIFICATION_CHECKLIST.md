# Items and Skills Verification Checklist

This document helps verify that all items and skills work as their descriptions state.

---

## 📦 ITEMS VERIFICATION

### ✅ **Fruit Juice**
- **Description:** "Heals 30 HP to all party members"
- **Implementation:** Lines 2099-2111 in CombatUI.cs
  - ✅ Heals ALL party members for 30 HP
  - ✅ Skips dead party members
- **Status:** CORRECT ✅

### ✅ **Super Espress-O**
- **Description:** "Restores 50 SP and increases ally's action generation by 50% for 3 turns"
- **Implementation:** Lines 2113-2140 in CombatUI.cs
  - ✅ Restores 50 SP (sanity)
  - ✅ Boosts action speed by 50% for 3 turns
  - ✅ Targets allies only
- **Status:** CORRECT ✅

### ✅ **Shiny Bead**
- **Description:** "Deals 20 damage to target enemy"
- **Implementation:** Lines 2142-2159 in CombatUI.cs
  - ✅ Deals 20 damage
  - ✅ Targets enemies only
- **Status:** CORRECT ✅

### ✅ **Panacea**
- **Description:** "Heal target party member for 100HP and 100SP, remove all negative status effects"
- **Implementation:** Lines 2161-2212 in CombatUI.cs
  - ✅ Heals 100 HP
  - ✅ Heals 100 SP
  - ✅ Removes WEAKNESS, VULNERABLE, and SLOWED
- **Status:** CORRECT ✅

### ✅ **Tower Shield**
- **Description:** "Gives TOUGH to ally for 3 turns"
- **Implementation:** Lines 2214-2240 in CombatUI.cs
  - ✅ Applies TOUGH status
  - ✅ Lasts 3 turns
  - ✅ Targets allies only
- **Status:** CORRECT ✅

### ✅ **Pocket Sand**
- **Description:** "WEAKENS all target enemies"
- **Implementation:** Lines 2242-2263 in CombatUI.cs
  - ✅ Applies WEAKNESS to ALL enemies
  - ✅ Lasts 3 turns (duration not in description but implemented)
- **Status:** CORRECT ✅

### ✅ **Otherworldly Tome**
- **Description:** "Gives STRENGTH to all party members for 3 turns"
- **Implementation:** Lines 2265-2280 in CombatUI.cs
  - ✅ Applies STRENGTH to ALL party members
  - ✅ Lasts 3 turns
  - ✅ Skips dead party members
- **Status:** CORRECT ✅

### ✅ **Unstable Catalyst**
- **Description:** "Deals 40 damage to all enemies"
- **Implementation:** Lines 2282-2299 in CombatUI.cs
  - ✅ Deals 40 damage
  - ✅ Hits ALL enemies
- **Status:** CORRECT ✅

### ✅ **Ramen**
- **Description:** "Heals ally for 15 HP"
- **Implementation:** Lines 2301-2319 in CombatUI.cs
  - ✅ Heals 15 HP
  - ✅ Targets allies only
- **Status:** CORRECT ✅

---

## 🎯 SKILLS VERIFICATION

### **The Magician's Skills**

#### ✅ **Before Your Eyes**
- **Description:** "Reset target's action gauge to 0"
- **Cost:** 15 Mind
- **Implementation:** Lines 1064-1086 in CombatUI.cs
  - ✅ Resets target action to 0
  - ✅ Costs 15 sanity
  - ✅ Requires target (enemy or ally)
- **Status:** CORRECT ✅

#### ✅ **Fiend Fire**
- **Description:** "Deal 10 damage to a target 1-5 times randomly"
- **Cost:** 10 Mind
- **Implementation:** Lines 1088-1113 in CombatUI.cs
  - ✅ Deals 10 damage per hit
  - ✅ Hits 1-5 times randomly
  - ✅ Costs 10 sanity
  - ✅ Requires enemy target
- **Status:** CORRECT ✅

#### ✅ **Cleansing Wave**
- **Description:** "Remove all negative status effects from self and allies"
- **Cost:** 5 Mind
- **Implementation:** Lines 1468-1512 in CombatUI.cs
  - ✅ Clears negative statuses (WEAK, VULNERABLE, SLOW)
  - ✅ Affects self AND allies
  - ✅ Costs 5 sanity
  - ✅ No target required
- **Status:** CORRECT ✅ (just updated)

#### ✅ **Respite**
- **Description:** "Target ally recovers 20 HP and 20 Mind but becomes SLOW for 2 turns"
- **Cost:** 5 Mind
- **Implementation:** Lines 1514-1548 in CombatUI.cs
  - ✅ Heals 20 HP
  - ✅ Heals 20 SP
  - ✅ Applies SLOWED for 2 turns
  - ✅ Costs 5 sanity
  - ✅ Requires ally target
- **Status:** CORRECT ✅

---

### **The Fighter's Skills**

#### ✅ **Slam!**
- **Description:** "Deal 15-30 damage to all enemies"
- **Cost:** 15 Mind
- **Implementation:** Lines 1115-1137 in CombatUI.cs
  - ✅ Deals 15-30 damage (random)
  - ✅ Hits ALL enemies
  - ✅ Costs 15 sanity
  - ✅ No target required
- **Status:** CORRECT ✅

#### ⚠️ **Human Shield!**
- **Description:** "Protect an ally by taking all damage they would receive until your next turn"
- **Cost:** 0 Mind
- **Implementation:** Lines 1139-1158 in CombatUI.cs
  - ✅ Costs 0 sanity
  - ✅ Requires ally target
  - ⚠️ **NEEDS TESTING:** Guardian system implementation
- **Status:** NEEDS TESTING ⚠️

#### ✅ **What Doesn't Kill You**
- **Description:** "Deal 10 damage to an ally and give them STRENGTH (+50% attack) for 2 turns"
- **Cost:** 5 Mind
- **Implementation:** Lines 1160-1183 in CombatUI.cs
  - ✅ Deals 10 damage to ally
  - ✅ Applies STRENGTH for 2 turns
  - ✅ Costs 5 sanity
  - ✅ Requires ally target
- **Status:** CORRECT ✅

#### ✅ **Fortify**
- **Description:** "Heal self for 40 HP and gain TOUGH (50% damage reduction) for 2 turns"
- **Cost:** 10 Mind
- **Implementation:** Lines 1586-1609 in CombatUI.cs
  - ✅ Heals 40 HP (just updated)
  - ✅ Applies TOUGH for 2 turns
  - ✅ Costs 10 sanity
  - ✅ Self-targeting
- **Status:** CORRECT ✅ (just updated)

---

### **The Bard's Skills**

#### ✅ **Healing Words**
- **Description:** "Heal an ally for 70 HP and 50 sanity"
- **Cost:** 20 Mind
- **Implementation:** Lines 1184-1203 in CombatUI.cs
  - ✅ Heals 70 HP
  - ✅ Heals 50 SP (just updated)
  - ✅ Costs 20 sanity
  - ✅ Requires ally target
- **Status:** CORRECT ✅ (just updated)

#### ⚠️ **Crescendo**
- **Description:** "Make an ally AGILE (+50% action speed) for 2 turns. Targets allies only."
- **Cost:** 10 Mind
- **Implementation:** Lines 1205-1230 in CombatUI.cs
  - ✅ Applies AGILE status
  - ✅ Costs 10 sanity
  - ✅ Requires ally target
  - ⚠️ **NEEDS TESTING:** Verify AGILE gives exactly 50% speed boost for 2 turns
- **Status:** NEEDS TESTING ⚠️

#### ✅ **Primordial Pile**
- **Description:** "Deal 7-10 damage to a target enemy 3 times and apply WEAKNESS (-50% attack) for 2 turns. Costs 20 sanity."
- **Cost:** 20 Mind
- **Implementation:** Lines 1232-1267 in CombatUI.cs
  - ✅ Deals 7-10 damage per hit
  - ✅ Hits 3 times
  - ✅ Applies WEAKNESS for 2 turns
  - ✅ Costs 20 sanity
  - ✅ Requires enemy target
- **Status:** CORRECT ✅

#### ✅ **Encore**
- **Description:** "Instantly fills an ally's action bar to maximum. Costs 0 sanity."
- **Cost:** 0 Mind
- **Implementation:** Lines 1269-1293 in CombatUI.cs
  - ✅ Fills action bar to maximum
  - ✅ Costs 0 sanity
  - ✅ Requires ally target
- **Status:** CORRECT ✅

---

### **The Ranger's Skills**

#### ⚠️ **Piercing Shot**
- **Description:** "Deal 10-15 damage and apply Vulnerable status (50% more damage taken) for 2 turns."
- **Cost:** 10 Mind
- **Implementation:** Lines 1295-1323 in CombatUI.cs
  - ✅ Deals 10-15 damage
  - ✅ Applies VULNERABLE for 2 turns
  - ✅ Costs 10 sanity
  - ✅ Requires enemy target
  - ⚠️ **NEEDS TESTING:** Verify VULNERABLE increases damage by exactly 50%
- **Status:** NEEDS TESTING ⚠️

#### ✅ **Signal Flare**
- **Description:** "Remove all status effects from all enemies. Costs 5 sanity."
- **Cost:** 5 Mind
- **Implementation:** Lines 1325-1353 in CombatUI.cs
  - ✅ Clears ALL statuses from ALL enemies
  - ✅ Costs 5 sanity
  - ✅ No target required
- **Status:** CORRECT ✅

#### ✅ **Gaintkiller**
- **Description:** "Deal 60-80 damage to a target enemy. Costs 70 sanity."
- **Cost:** 70 Mind
- **Implementation:** Lines 1355-1379 in CombatUI.cs
  - ✅ Deals 60-80 damage (random)
  - ✅ Costs 70 sanity
  - ✅ Requires enemy target
- **Status:** CORRECT ✅

#### ⚠️ **Bola**
- **Description:** "Deal 2-4 damage to a target enemy and apply SLOWED (-50% action speed) for 2 turns. Costs 20 sanity."
- **Cost:** 20 Mind
- **Implementation:** Lines 1381-1466 in CombatUI.cs
  - ✅ Deals 2-4 damage
  - ✅ Applies SLOWED for 2 turns
  - ✅ Costs 20 sanity
  - ✅ Requires enemy target
  - ⚠️ **NEEDS TESTING:** Verify SLOWED reduces speed by exactly 50%
- **Status:** NEEDS TESTING ⚠️

---

## 🔍 HOW TO TEST

### In-Game Testing Steps:

1. **Start a battle** (any battle scene)

2. **For each item/skill:**
   - Note the character's current stats (HP, Mind, Action bar)
   - Note any status effects on all characters
   - Use the item/skill
   - Verify the effect matches the description
   - Check the combat log for confirmation

3. **Specific tests needed:**

   **Human Shield:**
   - Use on an ally
   - Have enemy attack that ally
   - Verify the Fighter takes the damage instead
   - Verify it expires on Fighter's next turn

   **Crescendo:**
   - Use on an ally
   - Observe their action bar fill rate
   - Should be 50% faster than normal
   - Verify it lasts exactly 2 turns

   **Piercing Shot:**
   - Use on an enemy
   - Note enemy's current HP
   - Have ally attack same enemy
   - Verify damage is 50% higher than normal
   - Verify it lasts exactly 2 turns

   **Bola:**
   - Use on an enemy
   - Observe their action bar fill rate
   - Should be 50% slower than normal
   - Verify it lasts exactly 2 turns

---

## 📊 SUMMARY

- **Total Items:** 9
  - ✅ Verified Correct: 9
  - ⚠️ Needs Testing: 0

- **Total Skills:** 16
  - ✅ Verified Correct: 12
  - ⚠️ Needs Testing: 4 (Human Shield, Crescendo, Piercing Shot, Bola)

**Overall:** 21/25 confirmed correct (84%), 4 need in-game testing to verify status effect percentages and guardian mechanics.

---

## 🐛 ISSUES FOUND

**None currently** - All descriptions match implementations based on code review. The 4 items marked "NEEDS TESTING" have correct implementations but require in-game verification to ensure the status effect systems (AGILE, VULNERABLE, SLOWED) and guardian mechanics work as the descriptions state.

