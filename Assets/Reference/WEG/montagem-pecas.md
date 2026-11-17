# Montagem do W22 132M 4 polos: peças, encaixes e validação

Base: `Documento_Requisitos_Simulador_VR_Motor_W22.pdf` (seções 4, 5, 8, 10, 11) + `dimensoes.md` (pesquisa em PDFs oficiais WEG).
Legenda de origem de cada valor:
- **[WEG]** dado de catálogo lido, ver `dimensoes.md`.
- **[DERIV]** conta feita a partir de dado [WEG].
- **[ISO]** dimensão normalizada de rolamento (ISO 15). Não vem do catálogo WEG, conferir na ficha do fabricante.
- **[PROJ]** valor de projeto escolhido para o simulador. Não é dado do motor real e deve ser tratado como simplificação didática.

## 1. Sistema de coordenadas (unidades em metros)
- Eixo Z = eixo do rotor, **+Z para o lado da ponta de eixo** (lado acionado). Y para cima. X para a direita de quem olha o lado acionado.
- Origem O no **ombro do eixo**, na face externa da tampa dianteira [PROJ].
- Base dos pés: y = -0,132 (H = 132 [WEG]). Eixo do rotor em y = 0.
- Furos dos pés: x = ±0,108 (A/2, A = 216 [WEG/DERIV]); z = -0,089 (C = 89 [WEG]) e z = -0,267 (C + B, B = 178 [WEG/DERIV]). Ø12 [WEG, leitura provável].
- Ponta de eixo: Ø38, de z = 0 a z = +0,080 (D e E [WEG]).
- Chaveta 10 x 8, comprimento 63 [WEG]. Rasgo com fundo a 33 do lado oposto do eixo (G [WEG]), logo profundidade do rasgo = 38 - 33 = 5 mm e a chaveta sobressai 3 mm [DERIV]. Posição ao longo do eixo não consta: centralizar (8,5 mm de folga em cada ponta) [PROJ].

## 2. Peças do motor (15 pares de peça + parafusos)
Cada peça tem **pivô no próprio ponto de encaixe** (requisito do documento, seção 11).

| ID | Peça | Etapa | Medida base | Origem | Orç. tri. |
|---|---|---|---|---|---|
| P01 | Rotor com eixo (Ø38 x 80 na ponta) | 1 | eixo conforme seção 1; corpo do rotor [PROJ] | WEG + PROJ | 1500 |
| P02 | Rolamento dianteiro 6308-ZZ (lado acionado) | 1 | furo 40, externo 90, largura 23 | ISO | 600 |
| P03 | Rolamento traseiro 6207-ZZ (lado oposto) | 1 | furo 35, externo 72, largura 17 | ISO | 500 |
| P04 | Carcaça com estator bobinado | 2 | H, A, B, C, K [WEG]; corpo aletado [PROJ] | WEG + PROJ | 9000 |
| P05 | Tampa traseira | 3 | diâmetro e espessura [PROJ] | PROJ | 2500 |
| P06 | Tampa dianteira (com aletas e bujão de dreno) | 4 | idem; aletas e bujão [WEG] | WEG + PROJ | 3000 |
| P07 | Ventilador | 5 | plástico em 63 a 132M [WEG/Brake]; diâmetro [PROJ] | PROJ | 1200 |
| P08 | Tampa defletora (aerodinâmica, ferro fundido) | 5 | forma [WEG]; medidas [PROJ] | WEG + PROJ | 4000 |
| P09 | Base da caixa de ligação (topo, B3T) | 6 | 2 entradas M32x1,5 [WEG]; caixa [PROJ] | WEG + PROJ | 2000 |
| P10 | Tampa da caixa de ligação (divisão diagonal) | 6 | idem | WEG + PROJ | 1500 |
| P11 | Chaveta 10 x 8 x 63 | 7 | seção 1 | WEG | 80 |
| P12 | Parafuso (um modelo reutilizado) | 3 a 6 | cabeça sextavada, M8 [PROJ] | PROJ | 100 |
| D01 a D08 | Distratoras (engrenagem, polia, acoplamento, correia, rolamento errado, porca, arruela, tampa de outro formato) | — | [PROJ] | PROJ | até 800 cada |

Total estimado: cerca de 27 mil triângulos do motor + distratoras, dentro do orçamento de 40 mil do guia. Parafusos: nº por tampa não consta na pesquisa; usar 4 por tampa [PROJ].

## 3. Pontos de encaixe (sockets no motor-base)
Todos usam `XRSocketInteractor` com filtro por ID de peça. Tolerância de snap [PROJ]: 6 cm de posição e 25° de orientação (valor generoso, requisito de riscos do documento).

| Socket | Aceita | Posição (m) | Etapa |
|---|---|---|---|
| S-ROT-DE / S-ROT-NDE | P02 / P03 no eixo | z = +0,012 / z = -0,300 [PROJ] | 1 |
| S-CARC | P01 já montado em P04 | eixo de P04 em z = 0 | 2 |
| S-TR | P05 | face traseira de P04 | 3 |
| S-DI | P06 | face dianteira de P04 | 4 |
| S-VENT / S-DEF | P07 / P08 | z traseiro, atrás de P05 | 5 |
| S-CX-B / S-CX-T | P09 / P10 | topo de P04 (y = +0,134, raio da carcaça [DERIV de HC 266, linha SP, a validar]) | 6 |
| S-CHAV | P11 | rasgo na ponta de eixo | 7 |

Parafusos: 4 sockets por tampa e 4 na caixa, aceitando P12.

## 4. Bancada e caixa
- Caixa à direita do usuário com P01 a P12 e D01 a D08 em posição aleatória (RF01).
- Bancada com silhuetas brancas por etapa (RF03): uma silhueta por peça, rotulada só por número de etapa. Silhueta = cópia da malha com material de contorno branco (cópia invertida ou emissivo), sem textura.
- Motor-base à frente do usuário só depois de todas as silhuetas preenchidas (RF07, RF08).

## 5. Validação a cada encaixe (RF09)
Cada peça carrega `PartData` (ScriptableObject): `id`, `etapa`, `ordem`, `tipo` (real ou distratora).

| Condição no encaixe | Resultado | Borda | Pontos (proposta do documento) |
|---|---|---|---|
| ID certo, socket certo, ordem certa | Aceito | Verde | 0 |
| ID certo, socket certo, fora da ordem da etapa | Aceito com aviso | Amarela | -20 |
| ID errado ou socket errado, ou distratora | Aceito sem travar (RF05) | Vermelha | -40 |
| Peça removida ou corrigida | Estado recalculado | some (RF06) | — |

Contagem uma vez por tentativa de encaixe, não contínua (seção 7 do documento). A etapa conclui quando todas as peças dela estão em borda verde (RF10). Cores acompanhadas de ícone ou vibração (acessibilidade, seção 6).

## 6. Máquina de estados
`Inicio -> Organizacao -> Montagem(etapa 1..7) -> Painel(etapa n) -> Resultado`
- Cronômetro inicia no primeiro contato com uma peça e pausa no painel (a confirmar, seção 15 do documento).
- Dados das etapas e textos em ScriptableObject (`StepData`), conforme RNF.

## 7. Ordem de montagem e coerência com o motor real
- Etapas 1 a 7 como no documento. Rolamento dianteiro = 6308-ZZ (lado acionado), traseiro = 6207-ZZ [WEG].
- O documento marca a sequência como didática; a ordem industrial real não foi encontrada em fonte pública. Validar com o manual de IOM da WEG.
- Olhais e V-ring duplo não entram: na linha Prime só a partir da 225S/M [WEG]. Para o 132M padrão a pesquisa não confirmou.

## 8. O que ainda bloqueia fidelidade total
- Cor do W22 padrão (RAL/Munsell), número e altura das aletas, diâmetro externo da carcaça, L total, medidas da caixa de ligação e da placa: só no catálogo padrão (PDF de 13,4 MB ainda não baixado).
- Sem esses dados, o modelo usa valores [PROJ] e o revisor registra cada um como pendência, não como erro.
