-- Run with the local game server stopped. This script never replaces existing inventory items.
-- psql -v ON_ERROR_STOP=1 -v skin_character=testgmDK -f cosmetic-armor-set315.sql openmu
\if :{?skin_character}
\else
\echo 'Set skin_character to the character receiving the five cosmetic items.'
\quit 3
\endif
BEGIN;
SELECT pg_advisory_xact_lock(hashtext('skin315:' || lower(:'skin_character')));
CREATE TEMP TABLE skin_target ON COMMIT DROP AS
SELECT c."Id", c."InventoryId", d."GameConfigurationId"
FROM data."Character" c
JOIN data."Item" i ON i."ItemStorageId" = c."InventoryId"
JOIN config."ItemDefinition" d ON d."Id" = i."DefinitionId"
WHERE lower(c."Name") = lower(:'skin_character')
GROUP BY c."Id", c."InventoryId", d."GameConfigurationId";
DO $$ BEGIN
    IF (SELECT count(*) FROM skin_target) <> 1 THEN
        RAISE EXCEPTION 'Expected exactly one character and game configuration; no changes were made';
    END IF;
END $$;
CREATE TEMP TABLE skin_definitions ON COMMIT DROP AS
SELECT part, 7 + part AS item_group, name,
       ('00000080-' || lpad(to_hex(7 + part),4,'0') || '-013b-0000-000000000000')::uuid AS definition_id,
       ('00000086-' || lpad(to_hex(240 + part),4,'0') || '-0000-0000-000000000000')::uuid AS slot_id
FROM (VALUES (0,'Helm'),(1,'Armor'),(2,'Pants'),(3,'Gloves'),(4,'Boots')) v(part,name);
DO $$ BEGIN
    IF EXISTS (
        SELECT 1 FROM skin_definitions s JOIN config."ItemDefinition" d
        ON d."Group"=s.item_group AND d."Number"=315
        WHERE d."Id"<>s.definition_id OR d."Name"<>('Set 315 Skin ' || s.name)
    ) THEN RAISE EXCEPTION 'An item id is already used by another item; refusing to overwrite'; END IF;
    IF EXISTS (
        SELECT 1 FROM skin_definitions s JOIN config."ItemSlotType" t ON t."Id"=s.slot_id
        WHERE t."ItemSlots" <> (240+s.part)::text
    ) THEN RAISE EXCEPTION 'A reserved cosmetic slot id is already in use'; END IF;
END $$;
INSERT INTO config."ItemSlotType" ("Id","ItemSlots","GameConfigurationId","Description")
SELECT s.slot_id,(240+s.part)::text,t."GameConfigurationId",'Set 315 Skin '||s.name
FROM skin_definitions s CROSS JOIN skin_target t ON CONFLICT ("Id") DO NOTHING;
INSERT INTO config."ItemDefinition"
("Id","ItemSlotId","GameConfigurationId","Number","Width","Height","DropsFromMonsters","IsAmmunition",
 "IsBoundToCharacter","Name","DropLevel","MaximumItemLevel","Durability","Group","Value","MaximumSockets",
 "IsDroppable","IsPersonalStoreSellable","IsRepairable","IsSellableToNpc","IsStorable","IsTradable")
SELECT s.definition_id,s.slot_id,t."GameConfigurationId",315,2,CASE WHEN s.part=1 THEN 3 ELSE 2 END,false,false,false,
       'Set 315 Skin '||s.name,0,0,1,s.item_group,0,0,false,true,false,false,true,true
FROM skin_definitions s CROSS JOIN skin_target t ON CONFLICT ("Id") DO NOTHING;
DO $$
DECLARE target_inventory uuid; skin record; free_slot smallint;
BEGIN
    SELECT "InventoryId" INTO STRICT target_inventory FROM skin_target;
    FOR skin IN SELECT s.*,d."Width" AS width,d."Height" AS height FROM skin_definitions s JOIN config."ItemDefinition" d ON d."Id"=s.definition_id ORDER BY s.part LOOP
        IF EXISTS (SELECT 1 FROM data."Item" WHERE "ItemStorageId"=target_inventory AND "DefinitionId"=skin.definition_id) THEN
            CONTINUE;
        END IF;
        SELECT candidate::smallint INTO free_slot FROM generate_series(12,75) candidate
        WHERE (candidate-12)%8+skin.width <= 8
          AND (candidate-12)/8+skin.height <= 8
          AND NOT EXISTS (
            SELECT 1 FROM data."Item" i JOIN config."ItemDefinition" d ON d."Id"=i."DefinitionId"
            WHERE i."ItemStorageId"=target_inventory AND i."ItemSlot" BETWEEN 12 AND 75
              AND (candidate-12)%8 < (i."ItemSlot"-12)%8+d."Width"
              AND (candidate-12)%8+skin.width > (i."ItemSlot"-12)%8
              AND (candidate-12)/8 < (i."ItemSlot"-12)/8+d."Height"
              AND (candidate-12)/8+skin.height > (i."ItemSlot"-12)/8
        ) ORDER BY candidate LIMIT 1;
        IF free_slot IS NULL THEN RAISE EXCEPTION 'Not enough free bag cells; entire grant rolled back'; END IF;
        INSERT INTO data."Item" ("Id","ItemStorageId","DefinitionId","ItemSlot","Durability","Level","HasSkill","SocketCount","PetExperience")
        VALUES (gen_random_uuid(),target_inventory,skin.definition_id,free_slot,1,0,false,0,0);
    END LOOP;
END $$;
SELECT c."Name",i."ItemSlot",d."Name" AS "Skin",i."Id"
FROM skin_target t JOIN data."Character" c ON c."Id"=t."Id"
JOIN data."Item" i ON i."ItemStorageId"=t."InventoryId"
JOIN skin_definitions s ON s.definition_id=i."DefinitionId"
JOIN config."ItemDefinition" d ON d."Id"=s.definition_id ORDER BY s.part;
COMMIT;
