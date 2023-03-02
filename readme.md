## BannerLord Mods

A collection of mods for Mount & Blade II: Bannerlord, by me.

### Banks

As of 1.1.x you have a few ways of creating residual income:

* Fiefs
* Caravans
* Workshops
* Trade Perk (Spring of Gold)

*Fiefs* are only available mid/late game depending on your play style, require active management, and are vulnerable to looters, raids and sieges.

*Caravan* parties seem to ignore all the skills and perks of your leader (max size 30) making them vulnerable to looters and enemy lords.

*Workshops* are stuck at level 1, with income that is highly volatile given they need a steady supply of both materials and buyers. Not to mention that a rival kingdom can happily seize your workshop if you become an enemy for a hot second.

*Spring of Gold* requires you to achieve 250 in trade skill, typically mid/late game and the interest (while amazing) caps out at 1,000 denars.

IMHO whether you are an independent kingdom, mercenary or sworn vassal all your investments are unavoidably vulnerable to being undone at the snap of the AI's fingers. Surely there must be a better way!

#### How it works

BannerLord Banks creates a moneylender in each city that will happily allow you to deposit as much gold as you like at a competitive rate (based on the town's prosperity).

"But wait!", you say, "the Catholic Church banned usury during the dark ages!" You are correct, but let's take a step back. There is no Catholic Church in this game and besides the game itself has a policy called `Forgiveness of Debts`.

> So, how much can you make?

The game currently has 30 days per season for a total of 120 game days per year. 

If we look at the perk `Font of Gold` and do some math (1% interest per day) we are looking at an annual yield of 120%. No one in their right mind would take out a loan (even from your charming lord) at a rate of 120% APY.

> So what rate should we target? 

If we assume that a VERY prosperous city is at or above 6000 prosperity and you have an investment in a city then you can expect to get the full 60% APY or about 0.5% daily interest. Half as good as the perk `Font of Gold`.

> Vulnerabilities

You do need to shop around to find a city that is already prosperous or is moving in the right direction. 

A city with 0 prosperity will yield you zero interest. So there may be reasons to move your gold from time to time.

Your investment cannot be:

* *Seized* during a war (since the moneylender is tied to the city, not the factions)
* *Sacked* by bandits (I don't have any plans to add bank robberies or unlawful seizures)
* *Stolen* by your spouse
* *Spent* by your clan members

Withdrawing your money can be done at any time for a modest fee (currently 10%).

To be clear, a well-functioning caravan, trade shop or perk WILL have better returns. 

I for one would rather pay my hard-working army with a safe/steady income from my low-yield investment(s). 

In addition, it felt super dumb that you are only a good trader/investor if you are actively slogging mules from town to town.

Each day a portion (currently 15%) of your earned interest will count as trade income, giving you a passive improvement to your trade skill.