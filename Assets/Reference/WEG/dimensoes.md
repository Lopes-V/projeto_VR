# WEG W22 trifásico, carcaça IEC 132M, 4 polos: dimensões e detalhes construtivos

Estado: **PARCIAL**. Contém só o que foi lido em PDFs oficiais da WEG (static.weg.net). O catálogo técnico padrão do W22 IE3 (o que importa para o modelo) não pôde ser lido nesta rodada (ver "Pendências"). Nenhuma medida foi estimada. Onde está "não confirmado", não há fonte lida.

Nenhum PDF foi salvo em `Assets/Reference/WEG/` (falta aprovação direta do usuário para download). O texto foi lido de cópias temporárias da ferramenta de leitura web, fora do projeto.

## Fontes usadas (todas oficiais, static.weg.net)

| Sigla | Documento | URL |
|---|---|---|
| [SP] | W22 Severe Process, IE3, brochura (mercado europeu/IEC), cód. 50058022. Linha **W22 Severe Process**, não é o W22 padrão. | https://static.weg.net/medias/downloadcenter/hf9/hd0/WEG-w22-severe-process-european-market-50058022-brochure-english-web.pdf |
| [BRK] | W22 Brake Motor, catálogo técnico (mercado australiano), cód. 50106985 rev. 00, 04/2021. Linha **freio**, mesma plataforma W22. | https://static.weg.net/medias/downloadcenter/h1c/h01/WEG-WMO-w22-brake-technical-catalog-50106985-english-web.pdf |
| [PRIME] | W22 Prime, catálogo técnico (Europa), cód. 50162928. Linha **Prime**, hoje só carcaças 225S/M a W315S/M. Serve só como informação de família W22. | https://static.weg.net/medias/downloadcenter/he9/hf1/WEG-WMO-W22-Prime-50162928-en.pdf |

Páginas: [SP] usa numeração impressa no rodapé; [BRK] e [PRIME] idem. Quando o rodapé foi ambíguo na extração de texto, está indicado.

## 1. Dimensões de montagem IEC, 132M (valores em mm)

Dois documentos WEG independentes concordam nestes valores.

| Cota | Valor | Fonte e página | Observação |
|---|---|---|---|
| H (altura do eixo) | 132 | [SP] p.9; [BRK] p.19 | confirmado |
| A (distância entre furos, largura) | 216 | [SP] p.9; [BRK] p.19 | confirmado |
| B (distância entre furos, comprimento) | 178 | [SP] p.9; [BRK] p.19 | confirmado |
| C (do ombro do eixo ao furo dianteiro) | 89 | [SP] p.9; [BRK] p.19 | confirmado |
| D (diâmetro do eixo, lado acionado) | 38 k6 | [SP] p.9; [BRK] p.19 | confirmado |
| E (comprimento do eixo) | 80 | [SP] p.9; [BRK] p.19 | confirmado |
| ES (comprimento útil da chaveta) | 63 | [SP] p.9; [BRK] p.19 | confirmado |
| F (largura da chaveta) | 10 | [SP] p.9; [BRK] p.19 | confirmado |
| G (distância do fundo do rasgo à face oposta do eixo) | 33 | [SP] p.9; [BRK] p.19 | confirmado |
| GD (altura da chaveta) | 8 | [BRK] p.19 | confirmado em [BRK]; em [SP] a coluna é ambígua |
| d1 (furo roscado no topo do eixo) | M12 (DM12) | [BRK] p.19; [SP] p.9 | confirmado |
| BB (comprimento do pé, total) | 225 | [SP] p.9; [BRK] p.19 | confirmado |
| K (diâmetro dos furos dos pés) | 12 | [SP] p.9 | **leitura provável**: a célula é mesclada com 100L a 132M/L no texto extraído. Confirmar no desenho |
| Rolamentos (lado acionado / oposto) | 6308-ZZ / 6207-ZZ | [SP] p.9; [BRK] p.19 | confirmado |

### Cotas que dependem da linha (não usar como "W22 padrão" sem validar)

Valores da linha **Severe Process** [SP] p.9, linha "132M" (tabela com células mescladas, leitura do texto extraído; valores consistentes em ordem, mas o desenho cotado não foi visto):

| Cota | [SP] 132M | Observação |
|---|---|---|
| AA (largura da aba do pé) | 51 | |
| AB (largura sobre os pés) | 248 | |
| AC (diâmetro/largura máxima do corpo) | 271 | se inclui tampa defletora: não confirmado |
| AD (altura do eixo até topo da caixa, no sentido da caixa) | 218 | |
| HA (espessura do pé) | 20 | |
| HC (altura da base ao topo da carcaça) | 266 | |
| HD (altura da base ao topo da caixa de ligação) | 354 | altura total do motor com caixa no topo, segundo a definição padrão do desenho |
| L (comprimento total) | 490 | só linha Severe Process |
| LC (comprimento total com ventilador/tampa) | 557 | só linha Severe Process |
| LL / LM (caixa de ligação) | 140 / 133 | |
| Entradas de cabo | 2 x M32x1,5 | [SP] p.9 |
| Peso, nível de ruído | varia com a potência | não coletado |

Para a linha **Brake** [BRK] p.19, 132M: AA 45,5; AB 248; AC 272; AD 218; HC 274; HD 319 (leitura ambígua); L 580. Esses valores incluem o freio e **não** servem para o motor padrão.

**Comprimento total L do W22 padrão 132M 4 polos: não confirmado.** Altura total e largura total do W22 padrão: não confirmado (use H + HD e AB/AC do catálogo padrão quando for lido).

## 2. Carcaça aletada

| Item | Dado | Fonte |
|---|---|---|
| Material | Ferro fundido FC-200 (EN GJL 200) | [PRIME] p.4 (família W22 Prime); [BRK] p.3 (W22 Brake) |
| Aletas | "projetadas para minimizar o acúmulo de líquidos e poeira" | [PRIME] p.4 |
| Número, espaçamento, altura das aletas | **não confirmado** (só em desenho cotado) | n/a |
| Diâmetro externo da carcaça | **não confirmado** (AC=271 na linha SP é a cota de envolvente, não o diâmetro da carcaça) | n/a |
| Estrutura | "reduz a dispersão do ar e melhora a refrigeração" | [BRK] p.2 |
| Pontos planos para acelerômetro | só carcaças 225S/M a W315S/M | [PRIME] p.5 |

## 3. Pés e furação

| Item | Dado | Fonte |
|---|---|---|
| Pés maciços (feitos junto com a carcaça) | confirmado | [PRIME] p.4 (Figure 3 "Solid feet"); [BRK] p.2 |
| A x B | 216 x 178 mm | [SP] p.9, [BRK] p.19 |
| Quantidade de furos | 4 (pela geometria A x B de motor com pés) | não há texto explícito lido; tratar como "não confirmado por texto" |
| K | 12 mm (provável, ver tabela 1) | [SP] p.9 |
| Planicidade dos pés | 0,127 mm garantida (Severe Process) | [SP] p.3 |

## 4. Tampa dianteira (lado acionado)

| Item | Dado | Fonte |
|---|---|---|
| Material | Ferro fundido FC-200 ("endshields" do mesmo material da carcaça) | [PRIME] p.6 |
| Aletas | A tampa dianteira tem aletas para dissipar calor (linha Prime) | [PRIME] p.6 |
| Graxeira (relubrificação) | presente em todas as carcaças (Severe Process) | [SP] p.3 |
| Bujão de dreno | na tampa dianteira (posição ilustrada na Figure 14 de [PRIME]) | [PRIME] p.7 |
| Dimensões da tampa/flange (versão com flange FF) | FF-265: LA 12, M 265, N 230, P 300, 4 furos, 45° (carcaça 132, Severe Process) | [SP] p.10 |
| Flange C-DIN | C-200: LA 15,5, M 165, N 130, P 200, rosca M10, 4 furos | [SP] p.10 |
| Dimensões da tampa do motor de pés padrão (diâmetro, espessura) | **não confirmado** | n/a |

## 5. Tampa traseira (lado oposto), tampa defletora e ventilador

| Item | Dado | Fonte |
|---|---|---|
| Tampa defletora | ferro fundido, desenho aerodinâmico para reduzir ruído e otimizar o fluxo entre as aletas | [PRIME] p.7 (3.10), Figure 15 |
| Grade de ventilação (número de furos/fendas, geometria) | **não confirmado** | n/a |
| Ventilador (linha Brake, 63 a 132M/L) | plástico | [BRK] p.3 |
| Ventilador (linha Severe Process) | bronze/plástico condutivo, antifaísca | [SP] p.3 |
| Ventilador do W22 padrão 132M | **não confirmado** (provável plástico, sem fonte padrão lida) | n/a |
| Resfriamento | TEFC, IC 411 | [PRIME] p.7 (4.1); [BRK] p.3 |
| Diâmetro/pás do ventilador | **não confirmado** | n/a |

## 6. Caixa de ligação

| Item | Dado | Fonte |
|---|---|---|
| Material | Ferro fundido FC-200 | [PRIME] p.5 (3.5); [BRK] p.3 |
| Divisão diagonal | confirmada na linha Prime ("diagonally split"); em W22 padrão a divisão diagonal é citada para 160M a 355A/B no guia original (fonte pública, não relida nesta rodada) | [PRIME] p.5 |
| Posição padrão | no topo (B3T, caixa no topo) | [PRIME] p.12 (6) |
| Posição alternativa | esquerda ou direita (opcional, Severe Process) | [SP] p.3 |
| Entradas de cabo para 132M | 2 x M32x1,5 (Severe Process) | [SP] p.9 |
| Entrada de cabo da caixa de acessórios | M20x1,5 | [PRIME] p.6 (3.7) |
| Bloco de terminais | resina BMC reforçada com fibra de vidro | [PRIME] p.6 (3.6) |
| Junta | junta de neoprene entre caixa e carcaça (Severe Process) | [SP] p.3 |
| Dimensões (comprimento, largura, altura da caixa) para 132M | **não confirmado**. A tabela "Terminal Box Drawings" em [SP] p.11 tem linha para 90 a 132 com valores (117, 140, 71, 80, 133, 117, 36,5, 54), mas o mapeamento de cada número para a letra da cota não pôde ser feito sem o desenho. | [SP] p.11 |

## 7. Eixo e chaveta

| Item | Dado | Fonte |
|---|---|---|
| D x E | 38k6 x 80 mm | [SP] p.9; [BRK] p.19 |
| Chaveta | 10 x 8 mm (F x GD), comprimento útil 63 mm (ES), G = 33 mm | [SP] p.9; [BRK] p.19 |
| Furo de centro no topo | M12 | [BRK] p.19 |
| Material do eixo | AISI 1040/45 (linha Brake) | [BRK] p.3 |
| Tipo de chaveta (A, B, borda arredondada) | não confirmado | n/a |

## 8. Placa de identificação

| Item | Dado | Fonte |
|---|---|---|
| Conteúdo (27 campos numerados) | código do motor, nº de fases, tensão, serviço, código IE, carcaça, IP, classe de isolamento, elevação de temperatura, frequência, potência, rpm, corrente, fator de potência, temperatura ambiente, fator de serviço, altitude, peso, rolamentos e graxa, diagrama de ligação, intervalos de relubrificação, selos de certificação, data de fabricação, nº de série, eficiência em carga parcial | [PRIME] p.7, Figure 17 |
| Posição | ilustrada na Figure 16 de [PRIME] p.7; descrição em palavras não extraível do texto. Pedir ao usuário a imagem ou olhar a figura. | [PRIME] p.7 |
| Formato (retangular, cantos) e dimensões | **não confirmado** | n/a |
| Material | aço inox gravado a laser (Severe Process) | [SP] p.3 |

## 9. Logo WEG

Posição, tamanho e relevo: **não confirmado**. Nenhuma fonte lida descreve o logo na tampa.

## 10. Olhais de içamento

| Item | Dado | Fonte |
|---|---|---|
| W22 Prime | olhais de série **a partir da carcaça 225S/M**; 4 furos roscados no topo da carcaça | [PRIME] p.5 (3.2) |
| Severe Process, desenho "Frames 90S to 132M/L" | nota: "para carcaças 132S, 132M/L e 132M o olhal será montado a 50°" | [SP] p.8 |
| W22 padrão 132M | **não confirmado** (provavelmente sem olhal de série; sem fonte) | n/a |

## 11. Bujões de dreno, respiro, parafusos, selos

| Item | Dado | Fonte |
|---|---|---|
| Dreno | bujão automático, IP56, na tampa dianteira (Prime); aço inox (Severe Process) | [PRIME] p.7 (3.9); [SP] p.3 |
| Respiro | não confirmado | n/a |
| Parafusos inox | opcional (linha Brake) | [BRK] p.3 |
| Parafusos das tampas | posicionados para não obstruir aletas (225S/M a 315, Prime) | [PRIME] p.6 |
| Selo lado acionado, 132M | vedação por lábio duplo (óleo) para 63 a 200L (Brake); WSeal (V-ring duplo com capa metálica) só de 225S/M em diante. **V-ring visível em 132M padrão: não confirmado** | [BRK] p.3; [PRIME] p.13 (7.2) |
| Selo lado oposto | lábio de borracha nitrílica (Brake) | [BRK] p.3 |

## 12. Pintura e acabamento

| Item | Dado | Fonte |
|---|---|---|
| Plano de pintura | 203A, semibrilho; primer alquídico óxido vermelho (1 demão) + esmalte sintético alquídico (1 demão). Espessuras lidas como 20 e 60 (unidade ilegível na extração, provavelmente µm) | [PRIME] p.13 (7.3) |
| Categoria de corrosão | C3 (ISO 12944-2) em Prime e Severe Process; C2 em Brake | [PRIME] p.13; [SP] p.3; [BRK] p.3 |
| Cor da linha Brake | Verde RAL 6002 | [BRK] p.3 |
| **Cor do W22 padrão (azul?)** | **não confirmado.** Nenhum dos três documentos lidos informa cor/RAL/Munsell para a linha padrão. | n/a |

Atenção: a cor verde RAL 6002 é da linha Brake. Não usar para o W22 padrão.

## Pendências (o que falta para fechar o modelo)

1. **Catálogo técnico W22 padrão (IE3)**: https://static.weg.net/medias/downloadcenter/h00/h9e/MKT-WMO-W22-Technical-Catalogue-Aus-R1-0523-web.pdf (13,4 MB, HTTP 200). Excede o limite de 10 MB da leitura web, então só dá para ler após baixar para `Assets/Reference/WEG/`. Também o catálogo africano: https://static.weg.net/medias/downloadcenter/h19/hb3/WEG-WMO-W22-50024206-en.pdf (13 MB). Esperado: tabela de dimensões completa de 132M (L, AC, HD, AB), cor, pintura, desenho da caixa de ligação, olhais.
2. Os links do guia para as brochuras europeia, IE2 e IE4 retornam 404 hoje (testados em 2026-11-17); usar a lista acima.
3. Se o usuário preferir não baixar: enviar o PDF ou imagens do desenho cotado do 132M e das aletas.
