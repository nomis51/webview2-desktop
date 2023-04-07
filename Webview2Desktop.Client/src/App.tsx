import './App.css'
import { useCallback, useEffect, useState } from 'react'
import { BackendService } from './services/backend';

function App() {
  const [randomNumber, setRandomNumber] = useState(0);
  const [randomNumber1, setRandomNumber1] = useState(0);

  useEffect(() => {
    BackendService.subscribe("random-number", res => {
      setRandomNumber1(res.data);
    })
  }, [])

  const getRandomNumber = useCallback(async () => {
    const response = await BackendService.send("randomNumber", {
      min: 5
    });
    setRandomNumber(response.data);
  }, []);

  return (
    <div className="App">
      {randomNumber > 0 && <p>The random number from C# is: {randomNumber}</p>}
      {randomNumber1 > 0 && <p>A random number getting updated by C#: {randomNumber1}</p>}

      <button onClick={() => getRandomNumber()}>Ask C# for a random number</button>
    </div>
  )
}

export default App
