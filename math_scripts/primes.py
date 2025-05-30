#!/usr/bin/env python3
import sys
import argparse

def generate_primes_sieve(limit):
    """
    Generates prime numbers up to 'limit' using the Sieve of Eratosthenes.
    Returns an empty list if limit < 2.
    """
    if limit < 2:
        return []

    # Create a boolean array "is_prime[0..limit]" and initialize
    # all entries it as true. A value in is_prime[i] will
    # finally be false if i is Not a prime, else true.
    is_prime = [True] * (limit + 1)
    is_prime[0] = is_prime[1] = False  # 0 and 1 are not prime numbers

    # Start with the first prime number, 2
    p = 2
    while (p * p <= limit):
        # If is_prime[p] is still true, then it is a prime
        if is_prime[p]:
            # Update all multiples of p greater than or equal to p*p
            for i in range(p * p, limit + 1, p):
                is_prime[i] = False
        p += 1

    # Collect all prime numbers
    primes = []
    for num in range(2, limit + 1):
        if is_prime[num]:
            primes.append(num)
    
    return primes

def main():
    parser = argparse.ArgumentParser(
        description="Generate prime numbers up to N (inclusive). "
                    "By default, outputs only prime numbers to stdout, one per line, suitable for piping. "
                    "Use --verbose for additional information on stderr."
    )
    parser.add_argument(
        "N", 
        type=int, 
        help="The upper limit (inclusive) for prime generation."
    )
    parser.add_argument(
        "--verbose",
        action="store_true",
        help="Print informational messages (like headers or count) to stderr."
    )
    
    args = parser.parse_args()
    limit = args.N

    if limit < 2:
        if args.verbose:
            print(f"Input N={limit}. No primes will be generated as the smallest prime is 2.", file=sys.stderr)
        # Output nothing to stdout, exit cleanly
        sys.exit(0)

    prime_numbers = generate_primes_sieve(limit) # Will not be empty if limit >= 2
    
    if args.verbose:
        print(f"Prime numbers up to {limit} (found {len(prime_numbers)}):", file=sys.stderr)

    for prime in prime_numbers:
        print(prime, flush=True) # This goes to stdout

if __name__ == "__main__":
    main()
