# Guia de estilo: motor WEG W22 trifásico

Fonte única de verdade para os agents `weg-design-researcher`, `weg-motor-modeler` e `weg-fidelity-reviewer`.
Todo valor marcado **[VALIDAR]** ainda não foi confirmado em catálogo oficial e não pode ser usado como fato.

## Estado do material de referência
- O usuário não tem fotos, catálogo nem CAD oficial. A fidelidade depende de coletar referência (ver `weg-design-researcher`).
- Catálogos públicos (baixar e salvar em `Assets/Reference/WEG/` só com aprovação do usuário, fora do build):
  - **Link que funciona (testado 2026-11-17, 13,4 MB)**, catálogo técnico W22 IE3: https://static.weg.net/medias/downloadcenter/h00/h9e/MKT-WMO-W22-Technical-Catalogue-Aus-R1-0523-web.pdf
  - Catálogo técnico W22 africano (13 MB): https://static.weg.net/medias/downloadcenter/h19/hb3/WEG-WMO-W22-50024206-en.pdf
  - Lidos na rodada 1 (ver `Assets/Reference/WEG/dimensoes.md`): W22 Severe Process (https://static.weg.net/medias/downloadcenter/hf9/hd0/WEG-w22-severe-process-european-market-50058022-brochure-english-web.pdf), W22 Brake (https://static.weg.net/medias/downloadcenter/h1c/h01/WEG-WMO-w22-brake-technical-catalog-50106985-english-web.pdf), W22 Prime (https://static.weg.net/medias/downloadcenter/he9/hf1/WEG-WMO-W22-Prime-50162928-en.pdf)
  - Links antigos que retornam 404 em 2026-11-17: brochuras europeia 50025712 (h33/h4a), IE2 (he7/h40) e IE4 (h59/h8b).
  - Modelo 3D gratuito de referência (verificar licença antes de usar): https://www.cgtrader.com/free-3d-models/engineering-parts/weg/w22-general-purpose

## Fatos já encontrados em fontes públicas (alta confiança)
- Carcaça, caixa de ligação e tampa defletora em **ferro fundido**.
- Motor **TEFC** (totalmente fechado, ventilado externamente), IP55 (opções IP56, IP65, IP66).
- Linha IEC com carcaças de **71 a 355** (a brochura africana começa em 63).
- Pés **maciços** (feitos junto com a carcaça).
- Caixa de ligação pode ficar **no topo, à esquerda ou à direita**. Nas carcaças 160M a 355A/B a caixa tem **divisão diagonal**.
- Tampa defletora com desenho **aerodinâmico**, para aumentar o fluxo de ar.

## Fatos confirmados em PDF oficial (detalhes, fontes e páginas em `Assets/Reference/WEG/dimensoes.md`)
- 132M, mesmas cotas em dois catálogos WEG (Severe Process p.9 e Brake p.19): H 132, A 216, B 178, C 89, D 38k6, E 80, chaveta F 10 x GD 8, ES 63, G 33, rolamentos 6308-ZZ / 6207-ZZ. K = 12 é leitura provável (célula mesclada em Severe Process p.9).
- Ferro fundido FC-200 (EN GJL 200) na carcaça, caixa de ligação e tampas (Prime p.4-6 e Brake p.3).
- Caixa de ligação padrão no topo (B3T), com divisão diagonal (Prime p.5 e p.12). Entradas 2xM32x1,5 para 132M (Severe Process p.9).
- Tampa defletora de ferro fundido com desenho aerodinâmico (Prime p.7). Tampa dianteira com aletas (Prime p.6).
- Dreno automático IP56 na tampa dianteira (Prime p.7).
- Plano de pintura 203A semibrilho, primer alquídico óxido vermelho mais esmalte alquídico, categoria C3 ISO 12944-2 (Prime p.13).
- Olhais de série a partir da 225S/M na linha Prime (p.5). WSeal (V-ring duplo) só a partir da 225S/M (Prime p.13).
- Atenção: a cor Verde RAL 6002 é da linha **Brake**, não do W22 padrão (Brake p.3). Não usar no modelo.

## Ainda a confirmar **[VALIDAR]**
- Quantidade, espaçamento e altura das aletas e diâmetro externo da carcaça (só em desenho cotado; não confirmado).
- Do W22 padrão 132M 4 polos: L total, AB, AC, HD, HC e dimensões da caixa de ligação. Só há valores da linha Severe Process (L 490, AC 271, HD 354); não usar como padrão sem validar.
- Formato exato, posição e dimensões da placa de identificação; logo e relevo da tampa (o conteúdo da placa está confirmado em Prime p.7).
- Olhais no 132M padrão, bujão de respiro, V-ring no 132M padrão, grade de ventilação do defletor, ventilador (material e diâmetro) do W22 padrão.

## Regras de design (não sair do padrão)
1. Uma carcaça por vez. Escolher uma (sugestão: **IEC 132M**, motor de 4 polos) e modelar com as medidas da tabela do catálogo.
2. Silhueta: corpo cilíndrico aletado, tampa traseira com defletor e grade de ventilação, caixa de ligação retangular sobre o corpo, pés maciços com 4 furos, eixo com chaveta.
3. Proporções fixas: usar escala 1:1 em metros. Toda dimensão vem de `Assets/Reference/WEG/dimensoes.md`. Se não existir esse arquivo, parar e pedir referência. Itens "não confirmado" nesse arquivo não podem ser modelados por estimativa.
4. Nada de inventar detalhes "bonitos" que o motor real não tem. Beleza vem de acabamento: bordas chanfradas, normais corretas, materiais PBR, desgaste sutil opcional.
5. Materiais (exatamente 5, limite do orçamento): `WEG_Paint_Blue` (toda a pintura), `WEG_Steel` (eixo, chaveta, parafusos, chapas do rotor e estator), `WEG_Copper` (bobinas), `WEG_Dark` (ventilador plástico, furos, gravações) e `WEG_Aluminum` (anéis do rotor, blindagens dos rolamentos e placa de identificação). Azul confirmado: **RAL 5009** (W22 Premium, tabela de cores WEG 50102574), sRGB aproximado #2E5978, semibrilho.
6. Divisão da caixa de ligação: horizontal no 132M [PROJ]. A divisão diagonal só está confirmada para 160M a 355 e na linha Prime; para o 132M padrão continua [VALIDAR].

## Orçamento para Quest 3 (URP, Vulkan)
- Motor completo: **até 40 mil triângulos** (alvo 20 a 30 mil).
- Máximo de 5 materiais, atlas de textura até 2048 px, sem transparência custosa.
- Aletas por geometria modelada nas LODs 0 e 1, normal map na LOD 2.
- Eixo Z = eixo do rotor (+Z para a ponta de eixo), Y para cima. Origem do motor no ombro do eixo; base dos pés em y = -0,132.
- O simulador é de **montagem** (ver `Documento_Requisitos_Simulador_VR_Motor_W22.pdf`): cada peça é um prefab separado, com **pivô no ponto de encaixe**. Lista de peças, sockets e orçamento por peça em `Assets/Reference/WEG/montagem-pecas.md`.
- Valores marcados [PROJ] em `montagem-pecas.md` são simplificações didáticas permitidas pelo documento de requisitos (modelos low poly criados pela equipe). Devem ficar centralizados em um único lugar (`W22Dimensions.cs`) para troca rápida quando o catálogo padrão for lido.

## Convenções de projeto
- Modelos em `Assets/Models/WEG/`, materiais em `Assets/Materials/WEG/`, prefabs em `Assets/Prefabs/WEG/`.
- Nomes: prefabs de peça pelo ID do documento de requisitos (`P04_Carcaca_Estator`, `P09_Caixa_Ligacao_Base`, `D01_Engrenagem`...). Motor montado: `WEG_W22_132M_Montado`.
- Não mexer em `Assets/Samples/`, `Packages/` nem `ProjectSettings/` sem pedido do usuário.
