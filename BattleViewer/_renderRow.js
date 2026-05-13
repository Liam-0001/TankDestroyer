  function renderRow(result) {
    const mapName     = result.mapName ?? '—';
    const bots        = result.bots ?? [];
    const botInfo     = result.botInfo ?? [];
    const hasCrashed  = result.hasCrashed ?? false;
    const isStalemate = result.isStalemate ?? false;
    const survivors   = bots.filter(t => !t.destroyed);

    botInfo.forEach(bot => {
      const tank     = bots.find(t => t.ownerId === bot.ownerId);
      const tankDead = tank ? (tank.destroyed ?? false) : true;

      let wins = 0, losses = 0, stalemates = 0, crashes = 0;

      if (hasCrashed) {
        crashes++; losses++;
      } else if (bots.every(b => b.destroyed)) {
        // draw
      } else if (isStalemate && tank && !tankDead) {
        stalemates++;
      } else if (survivors.length === 1 && survivors[0].ownerId === bot.ownerId) {
        wins++;
      } else {
        losses++;
      }

      const rowKey = `${bot.name}||${mapName}`;
      const existingRow = document.querySelector(`tr[data-key="${CSS.escape(rowKey)}"]`);

      if (existingRow) {
        existingRow.querySelector('.w').textContent = parseInt(existingRow.querySelector('.w').textContent) + wins;
        existingRow.querySelector('.l').textContent = parseInt(existingRow.querySelector('.l').textContent) + losses;
        existingRow.querySelector('.s').textContent = parseInt(existingRow.querySelector('.s').textContent) + stalemates;
        existingRow.querySelector('.c').textContent = parseInt(existingRow.querySelector('.c').textContent) + crashes;
      } else {
        const color = (bot.color ?? '').startsWith('#') ? bot.color : `#${bot.color || '888888'}`;
        const tr = document.createElement('tr');
        tr.dataset.key = rowKey;
        tr.innerHTML = `
          <td><div class="bot-name"><span class="color-dot" style="background:${color}"></span>${bot.name}</div></td>
          <td>${mapName}</td>
          <td class="num wins w">${wins}</td>
          <td class="num losses l">${losses}</td>
          <td class="num stalemates s">${stalemates}</td>
          <td class="num crashes c">${crashes}</td>
        `;
        resultsBody.appendChild(tr);
      }
    });
  }