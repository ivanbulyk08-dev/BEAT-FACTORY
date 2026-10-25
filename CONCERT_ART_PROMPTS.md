# Промпти для концертів (стиль Dark Rage)

Фанати вже намальовані на картинці, а артиста гра ставить на сцену сама: так у нього працюють анімації й гардероб.
Тому на картинці має бути **натовп, але порожня сцена**, а LED-екран порожній. Свій намальований натовп гра в таких залах вимкне.

Залів 5, від найменшого: Club → Small Venue → Concert Hall → Arena → Stadium.

Як користуватися:
- Для кожного залу збирай промпт так: **СПІЛЬНИЙ СТИЛЬ** + **КОМПОЗИЦІЯ** + опис залу. Негативний промпт — з блоку **NEGATIVE**.
- Формат: вертикальний 9:16, 1440×2560 (або 1080×1920).

---

## СПІЛЬНИЙ СТИЛЬ
```
Stylized 2D mobile game concert background, same style as a premium hip-hop idle game: clean bold dark outlines, cel shading with soft gradients, anime-influenced urban illustration, "dark rage" underground rap aesthetic. Palette: deep blacks and dark violet base, glowing neon purple (#9B30FF) and magenta stage light, white spotlight beams cutting through haze, small cyan accents, chrome details. Gothic cross and spiky star motifs in the stage design. High contrast, moody night concert lighting, crisp readable shapes, polished game key-art quality.
```

## КОМПОЗИЦІЯ
```
Vertical 9:16 portrait composition for a phone screen, camera at audience eye level looking at the stage, slightly from below. A large EMPTY LED screen above and behind the stage, plain dark purple glowing rectangle with no image and no text, centred, occupying the area 8–35% from the top. The stage floor is clearly visible in the middle of the image (around 55–65% from the top) and the centre of the stage is EMPTY and well lit by spotlights (the performer will be added by the game). A DJ booth with a laptop can stand at the back of the stage on one side. The bottom 35% of the image is a dense crowd of fans seen from behind and slightly from the side, facing the stage: many raised hands and smartphones with glowing purple screens, heads with dreadlocks, braids, red, lavender and bleached hair, caps, beanies and hoodies, the nearest fans large at the very bottom edge, the crowd getting smaller towards the stage. The crowd must NOT cover the stage floor: the top of the crowd ends just below the stage edge. Nobody on the stage. No text, no letters, no logos, no watermark, no UI.
```

## NEGATIVE
```
performer on stage, singer, rapper holding a microphone, person on the stage, crowd covering the stage, fans facing the camera, text, letters, words, logo, watermark, signature, UI, buttons, frame, border, image on the LED screen, photorealistic, 3D render, blurry, low detail, distorted perspective, fisheye, daylight
```

---

## 1. Club
```
A tiny underground hip-hop club. Low black ceiling with exposed pipes, purple-lit brick walls with graffiti of gothic stars, a small raised wooden stage only knee-high, two stacked speakers on each side, a small LED screen behind the stage, a DJ table with a laptop on the left (no DJ), two purple spotlights and one white spot, thick haze, neon "star" sign as a shape (no letters), a bar glow at the side. About 40 fans packed close to the stage, hands up, phones out. Intimate, raw, sweaty.
```

## 2. Small Venue
```
A black-box concert room. Black walls, metal lighting truss above the stage with purple wash lights and white moving-head spots, a medium LED wall behind the stage, a stage about one metre high with a black front and a thin purple LED edge, speaker stacks on both sides, a DJ booth with a laptop at the back right, metal barrier in front of the stage, haze and light beams. Around 100 fans behind the barrier, jumping, hands and phones up. Underground but professional.
```

## 3. Concert Hall
```
A big theatre concert hall. Tall stage with a dark curtain frame, a wide LED screen above the stage, rows of purple and magenta beam lights on trusses, balconies with dim lights on the sides of the hall, chrome gothic cross ornaments on the stage sides, a DJ riser with a laptop at the back, a barrier and stage monitors at the front edge, haze filled with light beams. A full floor of fans with phones up and people on the balconies as small silhouettes. Grand, dramatic.
```

## 4. Arena
```
An indoor arena concert. Huge stage with a giant curved LED wall, massive lighting rigs hanging from the roof with dozens of purple and white beams, laser lines across the hall, pyro towers on both sides of the stage (not firing), side screens, a long catwalk going from the centre of the stage towards the camera, a DJ platform at the back, tiers of seats full of tiny silhouettes and phone lights in the background, a huge packed floor crowd in front with hundreds of glowing phones and raised hands, fans on both sides of the catwalk. Epic, loud, famous.
```

## 5. Stadium
```
An open-air stadium night concert, the biggest show. A colossal stage structure with a gigantic LED screen and a spiky gothic-star shaped frame, towers of lights and speakers, purple and magenta searchlight beams shooting into a dark purple night sky, lasers, fireworks sparks high in the sky, the stadium stands far away full of tiny phone lights, a sea of fans on the field with thousands of glowing phones and raised hands fading into haze, a catwalk from the stage towards the camera. Legendary, the top of the world.
```

---

## Окрема картинка «як на прикладі» (обкладинка, лоадінг-екран, реклама)
Якщо потрібна одна готова картинка з артистом і фанами, як на твоєму прикладі (для промо, а не як фон у грі), використовуй цей промпт:
```
Stylized 2D mobile game concert key art, clean bold dark outlines, cel shading, anime-influenced urban illustration, "dark rage" underground rap aesthetic, vertical 9:16. A young rapper with long black dreadlocks, black puffer jacket with silver gothic cross embroidery, black t-shirt, silver cross chain necklace, wide black cargo pants with cross patterns and silver chains, chunky black-and-white sneakers, standing on a stage, holding a microphone to his mouth with one hand and pointing up with the other. Behind him a huge LED screen showing a close-up of his face in purple tones with an audio-equalizer graphic, a DJ with a laptop on the back of the stage, purple spotlight beams through haze, purple brick walls. In front, a crowd of fans seen from behind and the side, raising hands and phones with glowing purple screens, fans with dreadlocks, red and lavender hair, caps and hoodies. Palette: deep blacks, violet and magenta neon, white highlights. No text, no logos, no watermark.
```

---

## Порада
- Скидай мені готові картинки залів. Я поставлю їх у гру й підгоню під них екран, світло, місце артиста й натовп.
- Для кожного залу роби 2–3 варіанти, а я виберу ті, де сцена й порожній екран стоять найкраще.
- Якщо генератор все одно малює когось на сцені, допиши в промпт «empty stage, nobody on stage».

---

## Окремі фанати (щоб натовп рухався)
Щоб фанати рухалися кожен окремо (стрибали, махали руками, піднімали телефон), потрібні окремі фігури на прозорому фоні. Гра поставить їх перед намальованим натовпом і анімуватиме кожного окремо.

Що згенерувати: 1 лист на 8–12 фанатів, вид зі спини, кожен окремо, з відступами між ними. Варіанти:
- руки вгору;
- з телефоном;
- «коза»;
- стрибає;
- руки опущені.
```
Character sheet of 10 separate concert fans seen from behind, same style as a premium hip-hop idle game: clean bold dark outlines, cel shading, anime-influenced urban illustration, "dark rage" aesthetic. Each fan is a separate full upper body (head, shoulders, arms, down to the waist), standing apart from the others with empty space between them, no overlapping. Variety: long dreadlocks in black, purple, red, lavender and bleached pink, caps with a cross, beanies, black hoodies and tees, silver chains. Poses: both hands up, one hand holding a phone with a glowing purple screen, rock-horns hand sign, fist up, arms down, mid-jump. Purple concert lighting from the front (rim light on hair and shoulders). Plain flat pure green background (#00FF00), no shadows on the background, no text, no logo.
```
Зелений фон я приберу сам. Шахівницю замість прозорості не використовуй.

---

## Різні ракурси концерту (з 2-го залу: Small Venue, Concert Hall, Arena, Stadium)
Це окремі кадри, між якими камера перемикається під час шоу. Для кожного залу 4 ракурси, усього 16 картинок.

Кожен промпт збирай так: **СПІЛЬНИЙ СТИЛЬ** (з початку файлу) + **ПРАВИЛА РАКУРСІВ** + опис залу + опис ракурсу.

### ПРАВИЛА РАКУРСІВ
```
Vertical 9:16 portrait composition for a phone screen, same venue design, colours and lighting as the main view of this venue. Nobody on the stage (the performer is added by the game), any LED screen is an empty glowing purple rectangle with no image and no text. No text, no letters, no logos, no watermark, no UI.
```

### Опис залу (вставляй один)
- **Small Venue:** `a black-box concert room with metal lighting truss, purple wash lights, a medium LED wall, speaker stacks, a metal barrier, about 100 fans`
- **Concert Hall:** `a big theatre concert hall with a dark curtain frame, a wide LED screen, purple and magenta beam lights on trusses, balconies with fans, chrome gothic cross ornaments, a full floor of fans`
- **Arena:** `an indoor arena with a huge curved LED wall, massive light rigs, lasers, side screens, a long catwalk into the crowd, tiers of seats full of fans with phone lights`
- **Stadium:** `an open-air night stadium with a colossal stage and a spiky gothic-star frame, light towers, searchlights into a purple sky, fireworks, a sea of fans with phones on the field and full stands`

### Ракурс 1: зі сцени на натовп
```
Camera on the stage at the front edge, looking out over the crowd, slightly from above. The bottom 15% of the image is the dark stage floor edge with stage monitors and a few light fixtures. The rest is the crowd facing the camera: excited faces lit purple, open mouths singing, raised hands, rock-horns signs, phones pointed at the camera with glowing screens, fans with dreadlocks in black, red, purple and bleached pink, caps with crosses, hoodies, chains; the crowd fills the venue to the back with light beams and haze over them.
```

### Ракурс 2: збоку сцени (з-за куліс)
```
Camera at the side wing of the stage at stage height, looking across the empty stage towards the other side, with the crowd visible on the right side of the image below the stage edge. In the foreground left: side speaker stacks, cables, a flight case, a light stand slightly out of focus. The stage floor runs across the middle of the image and the centre of the stage is empty and lit by a strong spotlight. Beams and haze above.
```

### Ракурс 3: загальний план з глибини залу
```
Camera high up at the back of the venue (balcony / stands / sound desk tower), looking towards the far stage over the whole crowd. The stage is small in the upper middle of the image with its LED screen and lights, the stage is empty. Most of the image is the huge crowd seen from behind: thousands of heads, raised hands and glowing phone screens fading into haze, beams sweeping over them. Shows the scale of the show.
```

### Ракурс 4: низько біля сцени
```
Camera low at the barrier right in front of the stage, looking up at the stage front. The edge of the stage fills the middle of the image, the empty stage above with dramatic backlight, smoke and beams pointing down and towards the camera. In the bottom of the image: the backs of the heads and raised hands of the front-row fans at the barrier, phones up. Strong low-angle, heroic perspective.
```

Підказки:
- На ракурсах 2 і 4 сцена має бути порожньою, там гра поставить артиста.
- Скидай картинки з підписом «зал + номер ракурсу», наприклад «Arena 3», і я вставлю їх у камеру шоу.

---

## STADIUM: 4 ракурси (детальні промпти)
Кожен промпт уже повний, копіюй цілком. Генеруй по одній картинці, формат 9:16 (1440×2560 або 1080×1920).

### Stadium 1: зі сцени на натовп
```
Stylized 2D mobile game concert art, clean bold dark outlines, cel shading with soft gradients, anime-influenced urban illustration, "dark rage" underground rap aesthetic, deep blacks and dark violet base with glowing neon purple (#9B30FF) and magenta light, white spotlight beams through haze, chrome details, gothic cross and spiky star motifs. Vertical 9:16 portrait composition for a phone screen.
View from the front edge of a colossal open-air stadium stage at night, camera at stage height looking out over the audience, slightly from above. The bottom 15% of the image is the black glossy stage floor edge with two wedge stage monitors, a row of small purple LED floor lights and a metal barrier just beyond it. Right behind the barrier the front rows of fans face the camera, large and detailed: open mouths screaming the lyrics, eyes shining, raised hands, rock-horns signs, pointing fingers, smartphones held up filming with glowing purple screens, long dreadlocks in black, red, purple, lavender and bleached pink, black caps with a small silver cross, beanies, black hoodies and tees, silver cross chains and bracelets, sweat and confetti. Behind them an endless sea of people across the whole stadium field getting smaller into the distance, thousands of phone lights like stars, then the huge curved stadium stands rising on three levels all full of tiny phone lights. Above: dark purple night sky with clouds, purple and white searchlight beams sweeping up, laser lines, falling purple and silver confetti, a few firework bursts high in the sky. Epic, overwhelming, legendary. Nobody on the stage, no text, no letters, no logos, no watermark, no UI.
```

### Stadium 2: збоку сцени (з-за куліс)
```
Stylized 2D mobile game concert art, clean bold dark outlines, cel shading with soft gradients, anime-influenced urban illustration, "dark rage" underground rap aesthetic, deep blacks and dark violet base with glowing neon purple (#9B30FF) and magenta light, white spotlight beams through haze, chrome details, gothic cross and spiky star motifs. Vertical 9:16 portrait composition for a phone screen.
View from the side wing of a gigantic open-air stadium stage at night, camera at stage height. Left foreground, dark and partly out of focus: a tall black speaker line-array hanging from a steel tower, stacked black flight cases with silver corners, coiled cables, a moving-head light on a case, a big chrome spiky gothic-star sculpture on the tower glowing with purple LEDs. The huge black glossy stage floor runs diagonally from the bottom middle towards the far side, reflecting purple and white light, with a row of stage monitors and LED floor lights along its front edge; the centre of the stage is EMPTY and lit by a bright white spotlight circle (the performer will be added by the game). Part of the giant LED screen frame is visible at the upper left, plain glowing purple with no image. On the right below the stage edge: a packed crowd of fans with raised hands and glowing phones, then the endless stadium field and three tiers of stands full of phone lights under a dark purple night sky crossed by searchlight beams, lasers and smoke. No text, no letters, no logos, no watermark, no UI.
```

### Stadium 3: загальний план з трибун (з глибини стадіону)
```
Stylized 2D mobile game concert art, clean bold dark outlines, cel shading with soft gradients, anime-influenced urban illustration, "dark rage" underground rap aesthetic, deep blacks and dark violet base with glowing neon purple (#9B30FF) and magenta light, white spotlight beams through haze, chrome details, gothic cross and spiky star motifs. Vertical 9:16 portrait composition for a phone screen.
Wide aerial view from high up in the upper stands at the far end of a giant open-air stadium at night, looking towards the stage. The stage is in the upper middle of the image, smaller but clearly visible: a colossal black stage structure shaped like a spiky gothic star, a huge central LED screen (plain glowing purple rectangle, no image, no text) with two tall side screens, light towers on both sides, rows of beams and lasers fanning out from the stage into the night sky, fireworks bursting above. The stage floor is empty and lit; a long catwalk runs from the centre of the stage into the crowd ending in a small round platform. The whole stadium field is a sea of tens of thousands of fans with glowing purple phone lights, a sound-desk tower with screens in the middle of the field, and the curved stands on both sides packed with people and phone lights. In the bottom 20% of the image, close to the camera: the backs of heads and raised hands with phones of fans in the upper stands, dreadlocks in red, purple and bleached pink, caps with a small cross. Dark purple cloudy night sky over everything. Shows the enormous scale of the show. No text, no letters, no logos, no watermark, no UI.
```

### Stadium 4: низько біля сцени (від загородження)
```
Stylized 2D mobile game concert art, clean bold dark outlines, cel shading with soft gradients, anime-influenced urban illustration, "dark rage" underground rap aesthetic, deep blacks and dark violet base with glowing neon purple (#9B30FF) and magenta light, white spotlight beams through haze, chrome details, gothic cross and spiky star motifs. Vertical 9:16 portrait composition for a phone screen.
Low-angle view from the barrier right in front of a colossal open-air stadium stage at night, looking up. The black front of the stage fills the middle of the image with a row of moving-head lights, wedge monitors and purple LED floor lights on its edge; above it the EMPTY stage (the performer will be added by the game) with thick purple smoke, a gigantic curved LED screen behind it as a plain glowing purple surface with no image and no text, two towering speaker arrays, chrome spiky gothic-star sculptures on the side towers, huge circular light truss overhead with dozens of beams pointing down and out, laser lines and sparks of pyro fountains at both sides of the stage (not covering the centre), fireworks and confetti in the purple night sky above. In the bottom 30% of the image: the backs of the heads, shoulders and raised hands of the front-row fans at the metal barrier, smartphones held high filming the stage with glowing screens, long dreadlocks in red, purple, lavender and bleached pink, black caps with a small silver cross, silver chains and bracelets. Strong heroic perspective, massive and overwhelming. No text, no letters, no logos, no watermark, no UI.
```

NEGATIVE для всіх чотирьох:
```
performer on stage, singer, rapper, microphone stand in the centre, person on the stage, image on the LED screen, text, letters, words, logo, watermark, signature, UI, frame, border, split screen, collage, multiple panels, photorealistic, 3D render, blurry, low detail, daylight, indoor roof
```
