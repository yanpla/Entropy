# Entropy

An Among Us mod built with MiraAPI and Reactor. Hidden modifiers build entropy over
time and trigger anomalies for individual players. Tasks and kills reduce entropy;
meetings reset it.


## Manual checks

While hosting an active game, keys 1–8 trigger anomalies on yourself in registry
order. These shortcuts are enabled in all builds and ignored while chat is open.

- Check each anomaly, including appearance restoration and temporary object cleanup.
- Report a fake body: it should disappear without starting a meeting. Real reports
  should still work.
- Check task/kill rewards, passive entropy gain, and the reset after a meeting.
- Use a second client to check that illusions stay local and displacement is shared.
