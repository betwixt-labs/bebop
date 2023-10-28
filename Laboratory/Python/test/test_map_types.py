from schemas import M, S, SomeMaps
from uuid import UUID


def test_map_types():

    m1 = {True: False}
    m2 = {"key": {"key2": "val"}}
    m3 = [{1: [{True: S(x=1, y=2)}]}]
    m4 = [{"key": [2.0]}]
    m = M()
    m.a = 0
    m.b = 1
    m5 = {UUID("ff1ed055-1839-4d7f-84d3-5ca28fa298b9"): m}

    sm = SomeMaps(m1, m2, m3, m4, m5)

    encoded = SomeMaps.encode(sm)

    decoded = SomeMaps.decode(encoded)

    assert decoded.m1.get(True) == False
    assert decoded.m2.get("key") == {"key2": "val"}
    assert len(decoded.m3[0].get(1)) == 1
    assert decoded.m3[0].get(1)[0].get(True).y == 2
    assert decoded.m4[0].get("key")[0] == 2.0
    assert decoded.m5.get(UUID("ff1ed055-1839-4d7f-84d3-5ca28fa298b9")).a == 0
