-- 背包里的通货堆叠上限统一到 999（原来是 20 / 10 / 40 / 100 / 250 等）。
-- 只改 StackableCurrency 一类，并跳过 28 种「满堆自动转换」的物品（Chaos Shard 20 个 → Chaos Orb 等），
-- 否则它们的合成阈值会被一起抬到 999，裂片合成直接失效。
-- 上限本来就高于 999 的（金币 50000、rogue marker 25000 等）保持不动。
UPDATE "CurrencyItems" SET "StackSize" = 999
WHERE "StackSize" > 1 AND "StackSize" < 999
  AND ("FullStack_BaseItemTypesKey" IS NULL OR "FullStack_BaseItemTypesKey" < 0)
  AND "BaseItemTypesKey" IN (
      SELECT "_row" FROM "BaseItemTypes"
      WHERE "ItemClassesKey" = (SELECT "_row" FROM "ItemClasses" WHERE "Id" = 'StackableCurrency')
  );
