-- Atualiza custo_medio_hora (base do nível Pleno) dos cargos já cadastrados. O seed usa INSERT IGNORE e não sobrescreve.
-- Júnior = base/1,75 | Pleno = base | Sênior = base x1,5 | Especialista = base x2
UPDATE cargo SET custo_medio_hora = 110.00 WHERE id_cargo = 1;
UPDATE cargo SET custo_medio_hora = 85.00 WHERE id_cargo = 2;
UPDATE cargo SET custo_medio_hora = 120.00 WHERE id_cargo = 3;
UPDATE cargo SET custo_medio_hora = 100.00 WHERE id_cargo = 4;
UPDATE cargo SET custo_medio_hora = 70.00 WHERE id_cargo = 5;
UPDATE cargo SET custo_medio_hora = 100.00 WHERE id_cargo = 6;
UPDATE cargo SET custo_medio_hora = 75.00 WHERE id_cargo = 7;
UPDATE cargo SET custo_medio_hora = 120.00 WHERE id_cargo = 8;
UPDATE cargo SET custo_medio_hora = 170.00 WHERE id_cargo = 9;
UPDATE cargo SET custo_medio_hora = 160.00 WHERE id_cargo = 10;
UPDATE cargo SET custo_medio_hora = 160.00 WHERE id_cargo = 11;
